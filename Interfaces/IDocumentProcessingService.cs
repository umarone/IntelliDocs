using AIChatAssistant.Models.Documents;

namespace AIChatAssistant.Interfaces
{
    public interface IDocumentProcessingService
    {
        Task ProcessAsync(Document document);
        Task UpdateAsync(Document document);
        Task DeleteAsync(Guid documentId);
    }
}
