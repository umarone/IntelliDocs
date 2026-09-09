using AIChatAssistant.Models.Chat;
using System.Threading.Tasks;

namespace AIChatAssistant.Interfaces
{
    public interface IChatProvider
    {
        string Name { get; }
        Task<ChatResponse> ChatAsync(ChatRequest request);
        IAsyncEnumerable<StreamingChatChunk> StreamAsync(
        ChatRequest request);
    }
}
