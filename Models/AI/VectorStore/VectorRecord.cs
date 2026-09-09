namespace AIChatAssistant.Models.AI.VectorStore
{
    public class VectorRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid DocumentId { get; set; }

        public Guid ChunkId { get; set; }

        public int ChunkIndex { get; set; }

        public string Content { get; set; } = string.Empty;

        public float[] Vector { get; set; } = [];
        public string FileName { get; set; } = string.Empty;

        public DateTime CreatedOn { get; set; }
        public string ContentType { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; }
    }
}
