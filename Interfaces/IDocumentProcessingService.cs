using AIChatAssistant.Models.Documents;

namespace AIChatAssistant.Interfaces
{
    public interface IDocumentProcessingService
    {
        Task ProcessAsync(Document document);
    }
}
