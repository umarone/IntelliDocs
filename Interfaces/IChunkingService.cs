using AIChatAssistant.Models.Documents;

namespace AIChatAssistant.Interfaces
{
    public interface IChunkingService
    {
        List<DocumentChunk> CreateChunks(
            Guid documentId,
            string content);
    }
}
