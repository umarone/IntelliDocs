using AIChatAssistant.Models.AI.Embeddings;
using AIChatAssistant.Models.AI.VectorStore;
using AIChatAssistant.Models.Documents;

namespace AIChatAssistant.Interfaces
{
    public interface IVectorRecordFactory
    {
        List<VectorRecord> Create(
        Document document,
        List<DocumentChunk> chunks,
        EmbeddingResponse embeddings);
    }
}
