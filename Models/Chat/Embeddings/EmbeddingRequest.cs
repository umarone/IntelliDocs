using AIChatAssistant.Models.Documents;

namespace AIChatAssistant.Models.Chat.Embeddings
{
    public class EmbeddingRequest
    {
        public List<DocumentChunk> Chunks { get; set; } = new();
    }
}
