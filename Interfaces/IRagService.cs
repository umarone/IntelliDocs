using AIChatAssistant.Models.Chat;

namespace AIChatAssistant.Interfaces
{
    public interface IRagService
    {
        Task<ChatResponse> AskAsync(ChatRequest request);
    }
}
