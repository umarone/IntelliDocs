using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Chat;

namespace AIChatAssistant.Services.AI.Prompt
{
    public class GeneralPromptSection : IPromptSection
    {
        public int Order => 0;

        public IEnumerable<OllamaChatMessage> Build(
            ChatRequest request,
            PromptContext context)
        {
            if (context.IsRagMode)
            {
                yield break;
            }

            yield return new OllamaChatMessage
            {
                Role = "system",
                Content = "You are a helpful AI assistant."
            };
        }
    }
}
