using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Tools;

namespace AIChatAssistant.Interfaces
{
    public interface IToolRouter
    {
        Task<ToolRoutingDecision> DecideAsync(
        ChatRequest request, ToolRoutingContext? context = null);
    }
}
