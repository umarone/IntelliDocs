using AIChatAssistant.Interfaces;
using UglyToad.PdfPig;

namespace AIChatAssistant.Services.Documents.Loaders
{
    public class PdfDocumentLoader : IDocumentLoader
    {
        public string Name => ".pdf";
        public Task<string> LoadAsync(byte[] content)
        {
            using var stream = new MemoryStream(content);

            using var document =
                PdfDocument.Open(stream);

            var text = string.Join(
                Environment.NewLine,
                document.GetPages()
                    .Select(page => page.Text));

            return Task.FromResult(text);
        }
    }
}
