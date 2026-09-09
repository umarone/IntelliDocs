namespace AIChatAssistant.Models
{
    public class SearchFilter
    {
        public Guid? DocumentId { get; init; }
        public string? FileName { get; init; }

        public string? ContentType { get; init; }

        public DateTime? UploadedFrom { get; init; }

        public DateTime? UploadedTo { get; init; }
    }
}
