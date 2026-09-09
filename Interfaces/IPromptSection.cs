using AIChatAssistant.Models.Chat;
using AIChatAssistant.Services.AI.Prompt;
namespace AIChatAssistant.Interfaces
{
    public interface IPromptSection
    {
        int Order { get; }
        IEnumerable<OllamaChatMessage> Build(
            ChatRequest request,
            PromptContext context);
    }
}
