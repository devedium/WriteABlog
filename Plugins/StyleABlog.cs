using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;
using WriteABlog;

namespace Plugins
{
    public class StyleABlog
    {
        [SKFunction("return the image style of the cover")]
        public string CoverImageStyle()
        {
            return "Abstract Art";
            //example: Pixel Art | Abstract Art | Minimalist | Infographic | Pop Art | Photographic | Satirical Cartoon 
        }

        public string Topic(SKContext context, Blog blog, string input)
        {
            blog.Topic = input;
            context.Variables.Set("topic", input);
            return input;
        }

        public string Title(SKContext context, Blog blog, string input)
        {
            char[] trimChars = new char[] { '\"', '\r', '\n' };
            string result = input.Trim(trimChars);
            context.Variables.Set("title", result);
            blog.Title = result;
            return result;
        }

        public string Subtitle(SKContext context, Blog blog, string input)
        {
            char[] trimChars = new char[] { '\"', '\r', '\n' };
            string result = input.Trim(trimChars);
            context.Variables.Set("subtitle", result);
            blog.Subtitle = result; 
            return result;
        }

        public string TableOfContents(SKContext context, Blog blog, string input)
        {
            char[] trimChars = new char[] { '\"', '\r', '\n' };
            string result = input.Trim(trimChars);
            context.Variables.Set("TableOfContents", result);
            blog.Subtitle = result;
            return result;
        }
    }
}
