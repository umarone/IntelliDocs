using AIChatAssistant.Models.Chat;
using System.Threading.Tasks;

namespace AIChatAssistant.Services
{
    public interface IAIService
    {
        Task<string> GetResponseAsync(ChatRequest request);
        IAsyncEnumerable<StreamingChatChunk> StreamAsync(
    ChatRequest request);
    }
}
