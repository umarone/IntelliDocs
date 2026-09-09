using AIChatAssistant.Models.Chat;
using AIChatAssistant.Services.AI.Prompt;

namespace AIChatAssistant.Interfaces
{
    public interface IPromptComposer
    {
        List<OllamaChatMessage> Compose(
        ChatRequest request,
        PromptContext context);
    }
}
