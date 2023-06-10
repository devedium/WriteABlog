using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Spectre.Console;
using System.Net;

namespace WriteABlog
{
    public class TextCompletionRequest
    {
        public List<string> prompt { get; set; }
        public int max_tokens { get; set; }
        public double temperature { get; set; }
        public int top_p { get; set; }
        public int n { get; set; }
        public bool echo { get; set; }
        public double presence_penalty { get; set; }
        public double frequency_penalty { get; set; }
        public int best_of { get; set; }
        public string model { get; set; }
    }

    public class CompletionChoice
    {
        public string text { get; set; }
        public int index { get; set; }
        public string finish_reason { get; set; }
    }

    public class CompletionUsage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }

    public class Completion
    {
        public string id { get; set; }
        public string @object { get; set; }
        public long created { get; set; }
        public string model { get; set; }
        public List<CompletionChoice> choices { get; set; }
        public CompletionUsage usage { get; set; }
    }

    public class ImageGenereationRequest
    {
        public string prompt { get; set; } = string.Empty;
        public int n { get; set; }
        public string size { get; set; } = string.Empty;
        public string response_format { get; set; } = string.Empty;
        public string user { get; set; } = string.Empty;
    }

    public class ImageUrl
    {
        public string url { get; set; } = string.Empty;
    }

    public class ImageGenereation
    {
        public long created { get; set; }
        public List<ImageUrl> data { get; set; }
    }

    public class EmbeddingsRequest
    {
        public string model { get; set; }
        public string input { get; set; }
    }

    public class Embeddings
    {
        public string @object { get; set; }
        public List<Embedding> data { get; set; }
        public string model { get; set; }
        public Usage usage { get; set; }
    }

    public class Embedding
    {
        public string @object { get; set; }
        public float[] embedding { get; set; }
        public int index { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int total_tokens { get; set; }
    }

    internal class ChatGPTHandler: DelegatingHandler
    {        
        private ILogger _logger;
        private bool _chatGPT;
        public ChatGPTHandler(HttpMessageHandler innerHandler, ILogger logger, bool chatGPT)
            : base(innerHandler)
        {
            _logger = logger;
            _chatGPT = chatGPT;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;
            if (_chatGPT) {
                if (request.Content != null)
                {
                    string content = await request.Content.ReadAsStringAsync();

                    string absolutePath = request.RequestUri.AbsolutePath.TrimEnd('/');
                    
                    string json = string.Empty;
                    if (absolutePath == "/v1/completions")
                    {
                        var textCompletionRequest = JsonConvert.DeserializeObject<TextCompletionRequest>(content, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                        
                        if (textCompletionRequest != null)
                        {
                            AnsiConsole.MarkupLine("[purple]CHATGPT PROMPT:[/]");
                            AnsiConsole.MarkupLine(textCompletionRequest.prompt[0].EscapeMarkup());                            
                            AnsiConsole.MarkupLine("[purple]PLEASE ENTER CHATGPT RESPONSE (FINISH WITH CTRL+Z ON NEW LINE):[/]");

                            var lines = ReadMultipleLines();
                            
                            var text = string.Join("\n", lines);
                            AnsiConsole.WriteLine("");
                            Completion completion = new Completion()
                            {
                                @object = "text_completion",
                                model = textCompletionRequest.model,
                                choices = new List<CompletionChoice>
                                {
                                    new CompletionChoice()
                                    {
                                         text = text,
                                         index = 0,
                                         finish_reason = "stop"
                                    }
                                },
                            };
                            json = JsonConvert.SerializeObject(completion);
                        }
                    }
                    else if (absolutePath == "/v1/embeddings") //tbd:create embeddings locally
                    {
                        var embeddingsRequest = JsonConvert.DeserializeObject<EmbeddingsRequest>(content, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                        if (embeddingsRequest != null)
                        {
                            AnsiConsole.MarkupLine("[purple]EMBEDDINGS INPUT:[/]");
                            AnsiConsole.MarkupLine(embeddingsRequest.input.EscapeMarkup());
                            AnsiConsole.MarkupLine("");
                            Embeddings embeddings = new Embeddings()
                            {
                                @object = "list",
                                data = new List<Embedding>()
                                {
                                    new Embedding()
                                    {
                                        @object = "embedding",
                                        embedding = new float[3] { 0.0023064255f, -0.009327292f, -0.0028842222f },
                                        index = 0
                                    }
                                },
                                model = embeddingsRequest.model
                            };

                            json = JsonConvert.SerializeObject(embeddings);
                        }
                    }
                    else if (absolutePath == "/v1/images/generations")
                    {
                        var imageGenereationRequest = JsonConvert.DeserializeObject<ImageGenereationRequest>(content, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                        if (imageGenereationRequest != null)
                        {
                            AnsiConsole.MarkupLine("[purple]DALLE IMAGE DESCRIPTION:[/]");
                            AnsiConsole.MarkupLine(imageGenereationRequest.prompt.EscapeMarkup());
                            AnsiConsole.MarkupLine("");
                            ImageGenereation imageGenereation = new ImageGenereation()
                            {
                                data = new List<ImageUrl>
                                {
                                    new ImageUrl()
                                    {
                                         url = "https://image-url.com"
                                    }
                                },
                            };
                            json = JsonConvert.SerializeObject(imageGenereation);
                        }
                    }                
                    
                    HttpContent responseContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = responseContent;
                }
            }
            else {
                response = await base.SendAsync(request, cancellationToken);
            }

            return response;
        }

        static List<string> ReadMultipleLines()
        {
            var lines = new List<string>();
            string line;
            while ((line = System.Console.ReadLine()) != null)
            {
                lines.Add(line);
            }
            return lines;
        }
    }
}
