using AIChatAssistant.Models.AI.Embeddings;

namespace AIChatAssistant.Interfaces
{
    public interface IEmbeddingProvider
    {
        Task<EmbeddingResponse> GenerateEmbeddingsAsync(
        EmbeddingRequest request);
        Task<float[]> GenerateEmbeddingAsync(
        string text);
    }
}
