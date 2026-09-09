using AIChatAssistant.Models;
using AIChatAssistant.Models.AI.Search;

namespace AIChatAssistant.Interfaces
{
    public interface IKnowledgeRetriever
    {
        Task<IReadOnlyList<SearchResult>> RetrieveAsync(
       string question,
       string originalQuestion,
       SearchFilter? filter = null);
    }
}
