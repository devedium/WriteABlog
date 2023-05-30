using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.Extensions.Configuration;

namespace WriteABlog
{
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

            var builder = Kernel.Builder;

            builder.Configure(kernalConfig => {
                kernalConfig.AddOpenAITextCompletionService(modelId: "text-davinci-003", apiKey: apiKey);                
            });

            IKernel kernel = builder.Build();

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