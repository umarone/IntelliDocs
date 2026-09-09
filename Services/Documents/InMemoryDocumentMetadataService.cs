using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Documents;

namespace AIChatAssistant.Services.Documents
{
    public class InMemoryDocumentRepository : IDocumentRepository
    {
        private readonly Dictionary<Guid, Document> _documents
    = new();

        public Task SaveAsync(Document document)
        {
            _documents[document.DocumentId] = document;

            return Task.CompletedTask;
        }

        public Task<Document?> GetAsync(Guid documentId)
        {
            _documents.TryGetValue(
                documentId,
                out var document);

            return Task.FromResult(document);
        }

        public Task<List<Document>> GetAllAsync()
        {
            return Task.FromResult(
                _documents.Values.ToList());
        }

        public Task DeleteAsync(Guid documentId)
        {
            _documents.Remove(documentId);

            return Task.CompletedTask;
        }
    }
}
