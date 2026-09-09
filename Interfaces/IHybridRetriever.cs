using AIChatAssistant.Models;
using AIChatAssistant.Models.AI.Search;

namespace AIChatAssistant.Interfaces
{
    public interface IHybridRetriever
    {
        //Task<List<SearchResult>> SearchAsync(
        //string question,
        //SearchFilter? filter = null,
        //int topK = 5);
        Task<List<SearchResult>> SearchAsync(
             string searchQuery,
             string originalQuery,
             SearchFilter? filter = null,
             int topK = 5);

        Task<List<SearchResult>> RetrieveCandidatesAsync(
        string searchQuery,
        SearchFilter? filter = null,
        int topK = 10, float similarityThreshold = 0.5f, string queryType = "Unknown");

        Task<List<SearchResult>> RerankAsync(
        string originalQuery,
        IReadOnlyList<SearchResult> candidates,
        int topK);

    }
}
