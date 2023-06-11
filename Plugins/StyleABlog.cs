﻿using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using Spectre.Console;
using WriteABlog;

namespace Plugins
{
    public class StyleABlog
    {
        private bool _chatGPT = false;
        public StyleABlog(bool chatGPT) 
        {
            _chatGPT = chatGPT;
        }

        public Blog blog = new Blog();

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
                        "Satirical Cartoon",
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
