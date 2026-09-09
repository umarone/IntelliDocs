namespace AIChatAssistant.Interfaces
{
    public interface IQueryTransformer
    {
        Task<string> TransformAsync(
        string query,
        CancellationToken cancellationToken = default);
    }
}
