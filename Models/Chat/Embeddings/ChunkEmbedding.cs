namespace AIChatAssistant.Models.Chat.Embeddings;

public class ChunkEmbedding
{
    public Guid ChunkId { get; set; }

    public float[] Vector { get; set; } = [];
}
