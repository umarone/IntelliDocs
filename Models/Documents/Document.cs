namespace AIChatAssistant.Models.Documents
{
    public class Document
    {
        public Guid DocumentId { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public byte[] Content { get; set; } = [];
    }
}
