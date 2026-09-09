using AIChatAssistant.Interfaces;

namespace AIChatAssistant.Services.Documents.Factories
{
    public class DocumentLoaderFactory : IDocumentLoaderFactory
    {
        private readonly IEnumerable<IDocumentLoader> _loaders;

        public DocumentLoaderFactory(
            IEnumerable<IDocumentLoader> loaders)
        {
            _loaders = loaders;
        }

        public IDocumentLoader GetLoader(string fileExtension)
        {
            var loader = _loaders.FirstOrDefault(l =>
                l.Name.Equals(fileExtension,
                    StringComparison.OrdinalIgnoreCase));

            if (loader == null)
            {
                throw new InvalidOperationException(
                    $"No document loader registered for '{fileExtension}'.");
            }

            return loader;
        }
    }
}
