namespace AIChatAssistant.Models.Quadrant
{
    public class QdrantPayload
    {
        public Guid DocumentId { get; set; }

        public Guid ChunkId { get; set; }

        public int ChunkIndex { get; set; }

        public string Content { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public DateTime CreatedOn { get; set; }

        public string ContentType { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; }
    }
}
