namespace AIChatAssistant.Interfaces
{
    public interface IDocumentLoaderFactory
    {
        IDocumentLoader GetLoader(string fileExtension);
    }
}
