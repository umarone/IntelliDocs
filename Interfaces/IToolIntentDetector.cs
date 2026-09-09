using AIChatAssistant.Models.Chat;

namespace AIChatAssistant.Interfaces
{
    public interface IToolIntentDetector
    {
        bool ShouldEnableTools(ChatRequest request);
    }
}
