using System.ComponentModel.DataAnnotations;
namespace AIChatAssistant.Models.Database
{
    public class VectorDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        // JSON representation of float[]
        [Required]
        public string Embedding { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
