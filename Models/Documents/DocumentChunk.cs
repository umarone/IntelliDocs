namespace AIChatAssistant.Models.Documents
{
    public class DocumentChunk
    {
        public Guid Id { get; set; }

        public Guid DocumentId { get; set; }

        public int ChunkIndex { get; set; }

        public string Content { get; set; } = string.Empty;
    }
}
