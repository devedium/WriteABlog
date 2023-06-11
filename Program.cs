using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ImageGeneration;
using Microsoft.SemanticKernel.CoreSkills;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.SemanticFunctions;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Spectre.Console;

namespace WriteABlog
{
    public class Blog
    {
        public string Topic { get; set; } = "";
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public string CoverDescription { get; set; } = "";
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

            string apiKey = config.GetSection("apiKey").Value ?? "";

            bool chatGPT = args.Any(arg => arg.Equals("--chatgpt", StringComparison.InvariantCultureIgnoreCase));

            if (string.IsNullOrEmpty(apiKey))
            {
                AnsiConsole.MarkupLine("[red]Invalid API key![/]");
                return;                
            }

            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
                                                    builder.AddSimpleConsole(options =>
                                                    {
                                                        options.IncludeScopes = true;
                                                        options.SingleLine = true;
                                                        options.TimestampFormat = "HH:mm:ss ";                                                        
                                                    }));

            ILogger logger = loggerFactory.CreateLogger("WriteABlog");
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            ChatGPTHandler chatGPTHandler = new ChatGPTHandler(httpClientHandler, logger , chatGPT);
            var httpClient = new HttpClient(chatGPTHandler);
            var builder = Kernel.Builder
                                .WithOpenAITextEmbeddingGenerationService("text-embedding-ada-002", apiKey, httpClient: httpClient)
                                .WithOpenAITextCompletionService(modelId: "text-davinci-003", apiKey: apiKey, httpClient: httpClient)
                                .WithOpenAIImageGenerationService(apiKey: apiKey, httpClient: httpClient)                                                             
                                .WithMemoryStorage(new VolatileMemoryStore())
                                .WithLogger(logger);

            IKernel kernel = builder.Build();  

            string topic = AnsiConsole.Prompt<string>(new TextPrompt<string>("[green]PLEASE ENTER THE TOPIC:[/]")
                                                .Validate(t =>
                                                {
                                                    return !string.IsNullOrEmpty(t);
                                                }));            

            
            List<string> referenceUrls = new List<string>();
            string line;
            while ((line = AnsiConsole.Prompt<string>(new TextPrompt<string>("[blue]PLEASE ENTER THE REFERENCE URL:[/]").AllowEmpty())) != "")
            {
                referenceUrls.Add(line);
            }

            foreach (string referenceUrl in referenceUrls)
            {
                var page = await CrawlAsync(referenceUrl);
                if (!string.IsNullOrEmpty(page))
                {
                    var paragraphs = page.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Where(p => p.Length > 100);

                    int index = 0;
                    foreach (string paragraph in paragraphs)
                    {
                        char[] trimChars = new char[] { ' ', '\t' };
                        string result = paragraph.Trim(trimChars);
                        if (!string.IsNullOrEmpty(result))
                        {
                            index++;
                            await kernel.Memory.SaveInformationAsync(topic, result, $"{referenceUrl}:{index}");
                        }
                    }
                }
            }

            var writePlugin = kernel.ImportSemanticSkillFromDirectory("Plugins", "WriteABlog");
            var blogStyle = new Plugins.StyleABlog(chatGPT);
            var stylePlugin = kernel.ImportSkill(blogStyle, "StyleABlog");
            kernel.ImportSkill(new TextMemorySkill(collection: topic, relevance: "0.8", limit: "1"));

            var variables = new ContextVariables();
            variables.Set("input", topic);

            //var planner = new SequentialPlanner(kernel);
            //var plan = await planner.CreatePlanAsync($"Write A Blog with topic: {topic}");            
            //var ret = await kernel.RunAsync(plan);
            //Console.WriteLine("Result:");
            //Console.WriteLine(ret.Result);

            var skContext = await kernel.RunAsync(variables,
                            stylePlugin["Topic"],
                            writePlugin["Title"],
                            stylePlugin["Title"],
                            writePlugin["Subtitle"],
                            stylePlugin["Subtitle"],
                            writePlugin["Cover"],
                            stylePlugin["Cover"],
                            writePlugin["TableOfContents"],
                            stylePlugin["TableOfContents"]);

            IImageGeneration dallE = kernel.GetService<IImageGeneration>();
            var imageUrl = await dallE.GenerateImageAsync(blogStyle.blog.CoverDescription, 1024, 1024);
            blogStyle.blog.CoverUrl = imageUrl;

            if (!chatGPT)
            {
                using (WebClient client = new WebClient())
                {
                    string url = imageUrl;
                    Uri uri = new Uri(url);
                    string localPath = System.IO.Path.GetFileName(uri.LocalPath);

                    client.DownloadFile(url, localPath);
                }
            }

            string[] chapters = SplitIntoChapters(blogStyle.blog.TableOfContents);
            
            foreach (var chapter in chapters)
            {
                if (chapter.Contains("Table of Contents", StringComparison.InvariantCultureIgnoreCase)) continue;               
                
                variables.Set("chapter", chapter);
                skContext = await kernel.RunAsync(variables,
                            writePlugin["Chapter"],
                            stylePlugin["Chapter"]);
            }

            SaveBlogToFile(blogStyle.blog, "blog.md");            

        }

        static void SaveBlogToFile(Blog blog, string filePath)
        {
            StringBuilder sb = new StringBuilder();            
            sb.AppendLine($"# {blog.Title}");
            sb.AppendLine($"{blog.Subtitle}\r\n");            
            sb.AppendLine($"![{blog.Title}]({blog.CoverUrl})\r\n");
            sb.AppendLine($"{blog.TableOfContents}\r\n");
            foreach (string chapter in blog.Chapters)
            {
                sb.AppendLine(chapter);
            }

            using (StreamWriter sw = new StreamWriter(filePath))
            {
                sw.Write(sb.ToString());
            }
            AnsiConsole.MarkupLine("[red]----------BLOG----------[/]");
            AnsiConsole.Write(sb.ToString());
        }

        static string[] SplitIntoChapters(string blogContent)
        {
            // Split the content by the chapter number pattern (number followed by dot)
            string[] chapters = Regex.Split(blogContent, @"(?<=\n)\d+\.");

            // Remove leading and trailing white spaces from each chapter
            for (int i = 0; i < chapters.Length; i++)
            {
                chapters[i] = chapters[i].Trim();
            }

            return chapters;
        }

        static async Task<string> CrawlAsync(string url)
        {
            var web = new HtmlWeb();
            HttpStatusCode statusCode = HttpStatusCode.NoContent;
            string? contentType = null;
            web.PostResponse = (req, res) => { statusCode = res.StatusCode; contentType = res.ContentType; };
            var doc = web.Load(url);
            if (statusCode == HttpStatusCode.OK && contentType != null && contentType.Contains("text/html"))
            {
                var text = doc.DocumentNode.InnerText;

                // If the crawler gets to a page that requires JavaScript, it will stop the crawl
                if (text.Contains("You need to enable JavaScript to run this app."))
                {
                    Console.WriteLine($"Unable to parse page {url} due to JavaScript being required");
                    return string.Empty;
                }
                return text;
            }
            else
            {
                return string.Empty;
            }
        }

        async Task WriteABlog_v2(IKernel kernel)
        {
            Console.Write("Please enter the topic: ");
            string topic = Console.ReadLine()??"";

            var writePlugin = kernel.ImportSemanticSkillFromDirectory("Plugins", "WriteABlog");
            var stylePlugin = kernel.ImportSkill(new Plugins.StyleABlog(false), "StyleABlog");

            var variables = new ContextVariables();
            variables.Set("topic", topic);

            var title = await kernel.RunAsync(variables, writePlugin["Title"]);
            Console.WriteLine(title);
            variables.Set("title", title.Result);

            var subtitle = await kernel.RunAsync(variables, writePlugin["Subtitle"]);
            Console.WriteLine(subtitle);
            variables.Set("subtitle", subtitle.Result);

            var cover = await kernel.RunAsync(variables, writePlugin["Cover"]);
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

        async Task WriteABlog_v1(IKernel kernel)
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