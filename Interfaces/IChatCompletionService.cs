namespace AIChatAssistant.Interfaces
{
    public interface IChatCompletionService
    {
        Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);

        Task<string> CompleteStructuredAsync(
       string systemPrompt,
       string userPrompt,
       CancellationToken cancellationToken = default);
    }
}
