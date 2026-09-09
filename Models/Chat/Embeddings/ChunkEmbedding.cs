namespace AIChatAssistant.Models.AI.Embeddings
{
    public class ChunkEmbedding
    {
        public Guid ChunkId { get; set; }

        public float[] Vector { get; set; } = [];
    }
}
