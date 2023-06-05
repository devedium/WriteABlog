using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.AI.ImageGeneration;
using System.Net;
using System.Reflection;

namespace WriteABlog
{
    public class Blog
    {
        public string Topic { get; set; } = "";
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string CoverUrl { get; set; } = "";
        public string TableOfContents { get; set; } = "";
        public IList<string> Chapters { get; set; } = new List<string>();
    }
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
               .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("env") ?? "dev"}.json", optional: true)                
                .Build();

            string? apiKey = config.GetSection("apiKey").Value;

            var builder = Kernel.Builder
                                .WithOpenAITextCompletionService(modelId: "text-davinci-003", apiKey: apiKey)
                                .WithOpenAIImageGenerationService(apiKey: apiKey);

            IKernel kernel = builder.Build();            

            Console.Write("Please enter the topic: ");
            string? topic = Console.ReadLine();
            
            var plugin = kernel.ImportSemanticSkillFromDirectory("Plugins", "WriteABlog");
            var stylePlugin = kernel.ImportSkill(new Plugins.StyleABlog(), "StyleABlog");

            var context = new ContextVariables();
            context.Set("topic", topic);

            var title = await kernel.RunAsync(context, plugin["Title"]);
            Console.WriteLine(title);
            context.Set("title", title.Result);
            
            var subtitle = await kernel.RunAsync(context, plugin["SubTitle"]);
            Console.WriteLine(subtitle);
            context.Set("subtitle", subtitle.Result);            

            var cover = await kernel.RunAsync(context, plugin["Cover"]);
            Console.WriteLine(cover);            

            IImageGeneration dallE = kernel.GetService<IImageGeneration>();            
            var image = await dallE.GenerateImageAsync(cover.Result, 1024, 1024); 
            
            using (WebClient client = new WebClient())
            {
                string url = image;  // replace with your URL
                Uri uri = new Uri(url);
                string localPath = System.IO.Path.GetFileName(uri.LocalPath);

                client.DownloadFile(url, localPath);
            }
        }

        async Task WriteABlog(IKernel kernel)
        {
            Console.Write("Please enter the topic: ");
            string? topic = Console.ReadLine();
            string prompt = $"""
                Write a blog with the following topic:
                {topic}
                """;

            var promptConfig = new PromptTemplateConfig
            {
                Completion =
                {
                    MaxTokens = 4000, Temperature = 0.2, TopP = 0.5,
                }
            };

            var promptTemplate = new PromptTemplate(
                prompt, promptConfig, kernel
            );

            var functionConfig = new SemanticFunctionConfig(promptConfig, promptTemplate);

            var function = kernel.RegisterSemanticFunction("WriteABlog", functionConfig);

            var blog = await kernel.RunAsync(function);

            Console.WriteLine(blog);
        }
    }
}