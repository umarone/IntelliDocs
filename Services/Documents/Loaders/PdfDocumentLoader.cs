using AIChatAssistant.Interfaces;

namespace AIChatAssistant.Services.Documents.Loaders
{
    public class PdfDocumentLoader : IDocumentLoader
    {
        public string Name => ".pdf";

        public async Task<string> LoadAsync(byte[] stream)
        {
            throw new NotImplementedException();
        }
    }
}
