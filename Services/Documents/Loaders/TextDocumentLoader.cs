using AIChatAssistant.Interfaces;

namespace AIChatAssistant.Services.Documents.Loaders
{
    public class TextDocumentLoader : IDocumentLoader
    {
        public string Name => ".txt";

        public async Task<string> LoadAsync(byte[] content)
        {
            using var stream = new MemoryStream(content);
            using var reader = new StreamReader(stream);

            return await reader.ReadToEndAsync();
        }
    }
}
