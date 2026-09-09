using AIChatAssistant.Models.AI.Search;

namespace AIChatAssistant.Interfaces
{
    public interface IMmrSelector
    {
        IReadOnlyList<SearchResult> Select(
        IReadOnlyList<SearchResult> candidates,
        int topK,
        float lambda);
    }
}
