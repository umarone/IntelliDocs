using AIChatAssistant.Interfaces;
using AIChatAssistant.Models;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.AI.VectorStore;

namespace AIChatAssistant.Services.Documents.VectorStores
{
    public class InMemoryVectorStore : IVectorStore
    {
        private static readonly List<VectorRecord> _records = new();
        public Task SaveAsync(IEnumerable<VectorRecord> records)
        {
            _records.AddRange(records);

            return Task.CompletedTask;
        }

        public Task<List<SearchResult>> SearchAsync(float[] queryVector, SearchFilter? filter, int topK, float similarityThreshold)
        {
            var records = _records.AsEnumerable();
            if (filter?.DocumentId is Guid documentId)
            {
                records = records.Where(record =>
                    record.DocumentId == documentId);
            }
            var result = _records
         .Select(record => new SearchResult
         {
             Record = record,
             Similarity = CosineSimilarity(
                 queryVector,
                 record.Vector)
         })
         .OrderByDescending(x => x.Similarity)
         .Take(topK)
         //.Select(x => x.Record)
         .ToList();
            for (int i = 0; i < result.Count; i++)
            {
                result[i].Rank = i + 1;
            }
            return Task.FromResult(result);
        }
        private static float CosineSimilarity(
    float[] vector1,
    float[] vector2)
        {

            if (vector1.Length != vector2.Length)
            {
                throw new InvalidOperationException(
                    "Vectors must have the same dimensions.");
            }
            float dotProduct = 0;
            float magnitude1 = 0;
            float magnitude2 = 0;
            
            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];

                magnitude1 += vector1[i] * vector1[i];

                magnitude2 += vector2[i] * vector2[i];
            }

            magnitude1 = MathF.Sqrt(magnitude1);
            magnitude2 = MathF.Sqrt(magnitude2);

            return dotProduct / (magnitude1 * magnitude2);
        }

        public Task<List<VectorRecord>> GetChunksAsync(Guid documentId, int startChunkIndex, int endChunkIndex)
        {
            throw new NotImplementedException();
        }
    }
}
