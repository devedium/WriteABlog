using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Spectre.Console;
using WriteABlog;

namespace Plugins
{
    public class StyleABlog
    {
        private bool _chatGPT = false;
        private string? _targetAudience = null;
        public StyleABlog(bool chatGPT) 
        {
            _chatGPT = chatGPT;
        }

        public Blog blog = new Blog();

        [SKFunction("return the blog's target audience")]
        public string TargetAudience()
        {
            if (_targetAudience == null)
            {
                _targetAudience = AnsiConsole.Prompt<string>(new TextPrompt<string>("[green]PLEASE ENTER THE TARGET AUDIENCE (FOR EXAMPLE: Stay-at-home parents, Small business owners, ech-savvy individuals, Skilled programmers, C# beginners, 5th graders, etc):[/]")
                                                    .Validate(t =>
                                                    {
                                                        return !string.IsNullOrEmpty(t);
                                                    }));
            }
            return _targetAudience;
        }


        [SKFunction("return the image style of the cover")]
        public string CoverImageStyle()
        {          
            var style = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("SELECT [green]IMAGE STYLE[/]:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](MOVE UP AND DOWN TO REVEAL MORE STYLES)[/]")
                    .AddChoices(new[] {
                        "Pixel Art", "Abstract Art", "Minimalist",
                        "Infographic", "Pop Art", "Photographic",
                        "Satirical Cartoon","Impressionism","Expressionism",
                        "Cubism", "Surrealism", "Art Nouveau", "Romanticism", 
                        "Realism", "Baroque", "Renaissance Art", "Gothic Art", 
                        "Neoclassicism", "Fauvism", "Dada", "Optical Art",
                        "Art Deco", "Conceptual Art", "Digital Art", "Street Art"
                    }));
            return style;
        }

        [SKFunction("return the image prompt length limit")]
        public string CoverImagePromptLengthLimit()
        {
            if (_chatGPT)
            {
                return 400.ToString();
            }
            else { 
                return 1000.ToString(); 
            }
        }

        [SKFunction("blog topic")]
        public string Topic(SKContext context, string input)
        {
            blog.Topic = input;
            context.Variables.Set("topic", input);
            return input;
        }

        [SKFunction("blog title")]
        public string Title(SKContext context, string input)
        {
            char[] trimChars = new char[] { '\"', '\r', '\n' };
            string result = input.Trim(trimChars);
            context.Variables.Set("title", result);
            blog.Title = result;
            return result;
        }

        [SKFunction("blog subtitle")]
        public string Subtitle(SKContext context, string input)
        {
            char[] trimChars = new char[] { '\"', '\r', '\n' };
            string result = input.Trim(trimChars);
            context.Variables.Set("subtitle", result);
            blog.Subtitle = result; 
            return result;
        }

        [SKFunction("blog cover")]
        public string Cover(SKContext context, string input)
        {
            char[] trimChars = new char[] { '\"', '\r', '\n' };
            string result = input.Trim(trimChars);            
            blog.CoverDescription = result;
            return result;
        }

        [SKFunction("blog table of contents")]
        public string TableOfContents(SKContext context, string input)
        {
            char[] trimChars = new char[] { '\"', '\r', '\n' };
            string result = input.Trim(trimChars);
            context.Variables.Set("TableOfContents", result);
            blog.TableOfContents = result;
            return result;
        }

        [SKFunction("blog chapter content")]
        public string Chapter(SKContext context, string input)
        {
            char[] trimChars = new char[] { '\"', '\r', '\n' };
            string result = input.Trim(trimChars);            
            blog.Chapters.Add(result);
            return result;
        }
    }
}
