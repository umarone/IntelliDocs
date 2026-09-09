using AIChatAssistant.Interfaces;

namespace AIChatAssistant.Services.Documents.Loaders
{
    public class WordDocumentLoader : IDocumentLoader
    {
        public string Name => ".docx";

        public async Task<string> LoadAsync(byte[] stream)
        {
            throw new NotImplementedException();
        }
    }
}
