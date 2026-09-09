using System.ComponentModel.DataAnnotations;

namespace AIChatAssistant.Models.Database
{
    public class VectorRecordEntity
    {
        [Key]
        public Guid Id { get; set; }

        public Guid DocumentId { get; set; }

        public Guid ChunkId { get; set; }

        public int ChunkIndex { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public string VectorJson { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string FileName { get; set; } = string.Empty;

        public DateTime CreatedOn { get; set; }
        public string ContentType { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; }
    }
}
