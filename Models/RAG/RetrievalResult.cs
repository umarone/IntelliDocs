using AIChatAssistant.Models.AI.Search;

namespace AIChatAssistant.Models.RAG
{
    public class RetrievalResult
    {
        public IReadOnlyList<SearchResult> Results { get; init; } = [];

        public IReadOnlyList<string> InformationNeeds { get; init; } = [];
    }
}
