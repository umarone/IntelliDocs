namespace AIChatAssistant.Models.DTOs
{
    public class DocumentSummaryDto
    {
        public Guid DocumentId { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; }
    }
}
