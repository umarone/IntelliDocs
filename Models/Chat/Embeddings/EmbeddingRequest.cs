using AIChatAssistant.Models.Documents;

namespace AIChatAssistant.Models.AI.Embeddings
{
    public class EmbeddingRequest
    {
        public List<DocumentChunk> Chunks { get; set; } = new();
    }
}
