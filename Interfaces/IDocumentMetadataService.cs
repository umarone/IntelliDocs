using AIChatAssistant.Models.Documents;

namespace AIChatAssistant.Interfaces
{
    public interface IDocumentRepository
    {
        Task SaveAsync(Document document);

        Task<Document?> GetAsync(Guid documentId);

        Task<List<Document>> GetAllAsync();

        Task DeleteAsync(Guid documentId);
    }
}
