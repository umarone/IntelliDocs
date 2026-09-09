using AIChatAssistant.Models.AI.Search;

namespace AIChatAssistant.Interfaces
{
    public interface IContextualCompressor
    {
        Task<IReadOnlyList<SearchResult>> CompressAsync(
        string question,
        IReadOnlyList<SearchResult> results,
        CancellationToken cancellationToken = default);
    }
}
