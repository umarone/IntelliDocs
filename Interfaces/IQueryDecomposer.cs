using AIChatAssistant.Models.RAG;

namespace AIChatAssistant.Interfaces
{
    public interface IQueryDecomposer
    {
        Task<IReadOnlyList<string>> DecomposeAsync(
            string question,
            CancellationToken cancellationToken = default);

        Task<(
            IReadOnlyList<string> InformationNeeds,
            QueryComplexityResult ComplexityAnalysis)>
            DecomposeIfNeededAsync(
                string question,
                CancellationToken cancellationToken = default);
    }
}
