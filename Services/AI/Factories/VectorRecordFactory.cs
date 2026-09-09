using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.AI.Embeddings;
using AIChatAssistant.Models.AI.VectorStore;
using AIChatAssistant.Models.Documents;

namespace AIChatAssistant.Services.AI.Factories
{
    public class VectorRecordFactory : IVectorRecordFactory
    {
        public List<VectorRecord> Create(
            Document document,
            List<DocumentChunk> chunks,
            EmbeddingResponse embeddings)
        {
            var embeddingLookup =
                embeddings.Embeddings
                    .ToDictionary(e => e.ChunkId);

            var records = new List<VectorRecord>();

            foreach (var chunk in chunks)
            {
                if (!embeddingLookup.TryGetValue(
                        chunk.Id,
                        out var embedding))
                {
                    throw new InvalidOperationException(
                        $"Embedding not found for chunk '{chunk.Id}'.");
                }

                records.Add(new VectorRecord
                {
                    Id = Guid.NewGuid(),

                    DocumentId = document.DocumentId,

                    ChunkId = chunk.Id,

                    ChunkIndex = chunk.ChunkIndex,

                    Content = chunk.Content,

                    Vector = embedding.Vector,

                    FileName = document.FileName,

                    ContentType = document.ContentType,

                    UploadedAt = document.UploadedAt,

                    CreatedOn = DateTime.UtcNow
                });
            }

            return records;
        }
    }
}
