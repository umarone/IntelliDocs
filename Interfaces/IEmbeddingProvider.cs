using AIChatAssistant.Models.Chat.Embeddings;

namespace AIChatAssistant.Interfaces
{
    public interface IEmbeddingProvider
    {
        Task<EmbeddingResponse> GenerateEmbeddingsAsync(
        EmbeddingRequest request);
        Task<float[]> GenerateEmbeddingAsync(
        string text);

        Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts);
    }
}
