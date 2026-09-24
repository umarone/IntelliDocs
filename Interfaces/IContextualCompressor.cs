using AIChatAssistant.Models.AI.Search;

namespace AIChatAssistant.Interfaces
{
    public interface IContextualCompressor
    {
        Task<IReadOnlyList<SearchResult>> CompressAsync(
        string question,
        IReadOnlyList<SearchResult> contextResults,
        IReadOnlyList<SearchResult> finalResults,
        CancellationToken cancellationToken = default);
    }
}
