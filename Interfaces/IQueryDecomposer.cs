namespace AIChatAssistant.Interfaces
{
    public interface IQueryDecomposer
    {
        Task<IReadOnlyList<string>> DecomposeAsync(
        string question,
        CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>> DecomposeIfNeededAsync(
        string question,
        CancellationToken cancellationToken = default);
    }
}
