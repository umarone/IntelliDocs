using AIChatAssistant.Common;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.Chat;
using System.Text;

namespace AIChatAssistant.Services.AI.Prompt
{
    public class KnowledgePromptSection : IPromptSection
    {
        public int Order => 100;
        private readonly IPromptFormatter _promptFormatter;
        public KnowledgePromptSection(IPromptFormatter promptFormatter)
        {
            _promptFormatter = promptFormatter;
        }
        public IEnumerable<OllamaChatMessage> Build(ChatRequest request, PromptContext context)
        {
            if (context.SearchResults == null ||
                !context.SearchResults.Any())
            {
                yield break;
            }

            var lines = new List<string>();

            var index = 1;

            foreach (var result in context.SearchResults)
            {
                lines.Add($"Context {index}");
                lines.Add(new string('-', 40));
                lines.Add(result.Record.Content);
                lines.Add(string.Empty);

                index++;
            }

            yield return new OllamaChatMessage
            {
                Role = "system",
                Content = _promptFormatter.Build(
                    "Use the following context to answer the user's question.",
                    "Knowledge Context",
                    lines)
            };
        }       
    }
}
