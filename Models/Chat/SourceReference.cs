namespace AIChatAssistant.Models.Chat
{
    public class SourceReference
    {
        public int Rank { get; set; }

        public float Similarity { get; set; }

        public Guid DocumentId { get; set; }

        public int ChunkIndex { get; set; }
        public string FileName { get; set; }
        public int ContentLength { get; set; }
    }
}
