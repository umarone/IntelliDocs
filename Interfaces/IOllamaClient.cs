using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Ollama;

namespace AIChatAssistant.Interfaces
{
    public interface IOllamaClient
    {
        Task<OllamaChatResponse> SendAsync(OllamaChatRequest request);

        IAsyncEnumerable<StreamingChatChunk> StreamAsync(OllamaChatRequest request);
    }
}
