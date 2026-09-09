using AIChatAssistant.Models;
using AIChatAssistant.Models.AI.Search;

namespace AIChatAssistant.Interfaces
{
    public interface IKeywordSearchService
    {
        Task<List<SearchResult>> SearchAsync(
        string query,
        SearchFilter? filter,
        int topK);
    }
}
