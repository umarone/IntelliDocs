using AIChatAssistant.Models;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.AI.VectorStore;

namespace AIChatAssistant.Interfaces
{
    public interface IDocumentSearchService
    {
        Task<List<SearchResult>> SearchAsync(
           string question,
           SearchFilter? filter,
           int topK = 5);
    }
}
