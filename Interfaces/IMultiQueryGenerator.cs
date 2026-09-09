namespace AIChatAssistant.Interfaces
{
    public interface IMultiQueryGenerator
    {
        Task<IReadOnlyList<string>> GenerateAsync(
        string query,
        CancellationToken cancellationToken = default);
    }
}
