using AIChatAssistant.Models.AI.VectorStore;

namespace AIChatAssistant.Models.AI.Search
{
    public class SearchResult
    {
        public VectorRecord Record { get; set; } = default!;
        public float Similarity { get; set; }
        public float RrfScore { get; set; }
        public float RerankScore { get; set; }
        public int Rank { get; set; }

        public string SearchQuery { get; set; } = string.Empty;
        public string QueryType { get; set; } = string.Empty;
        public float QueryWeight { get; set; } = 1.0f;
    }
}
