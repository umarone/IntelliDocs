using AIChatAssistant.Models.Chat;

namespace AIChatAssistant.Services.Providers
{
    public interface IAIProvider
    {
        Task<ChatResponse> ChatAsync(ChatRequest request);
    }
}
