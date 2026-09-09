namespace AIChatAssistant.Interfaces
{
    public interface ICorrectiveQueryGenerator
    {
        Task<string> GenerateAsync(
       string question,
       string validationReason,
       CancellationToken cancellationToken = default);
    }
}
