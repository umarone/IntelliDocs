using AIChatAssistant.Models.AI.Search;

namespace AIChatAssistant.Interfaces
{
    public interface IReranker
    {
        Task<List<SearchResult>> RerankAsync(
       string question,
       IReadOnlyList<SearchResult> candidates,
       int topK);
    }
}
