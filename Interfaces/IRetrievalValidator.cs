using AIChatAssistant.Models.AI.Search;

namespace AIChatAssistant.Interfaces
{
    public interface IRetrievalValidator
    {
        Task<RetrievalValidationResult> ValidateAsync(
       string question,
       IReadOnlyList<SearchResult> results,
       CancellationToken cancellationToken = default);
    }
}
