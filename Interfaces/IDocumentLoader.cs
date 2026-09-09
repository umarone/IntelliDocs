namespace AIChatAssistant.Interfaces
{
    public interface IDocumentLoader
    {
        string Name { get; }

        Task<string> LoadAsync(byte[] bytes);
    }
}
