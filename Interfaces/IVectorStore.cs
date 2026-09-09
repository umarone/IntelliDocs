using AIChatAssistant.Models;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.AI.VectorStore;

namespace AIChatAssistant.Interfaces
{
    public interface IVectorStore
    {
        Task SaveAsync(IEnumerable<VectorRecord> records);

        //Task<List<VectorRecord>> SearchAsync(
        //    float[] queryVector,
        //    int topK,
        //    Guid? documentId);
        Task<List<VectorRecord>> GetChunksAsync(
        Guid documentId,
        int startChunkIndex,
        int endChunkIndex);
        Task<List<SearchResult>> SearchAsync(
        float[] queryVector,
        SearchFilter? filter,
        int topK, float similarityThreshold);
    }
}
