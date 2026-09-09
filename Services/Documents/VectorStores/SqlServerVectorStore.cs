using AIChatAssistant.Common;
using AIChatAssistant.Configuration;
using AIChatAssistant.Data;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.AI.VectorStore;
using AIChatAssistant.Models.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AIChatAssistant.Services.Documents.VectorStores
{
    public class SqlServerVectorStore : IVectorStore
    {
        private readonly AppDbContext _context;
        private readonly RagOptions _options;
        public SqlServerVectorStore(AppDbContext context, IOptions<RagOptions> options)
        {
            _context = context;
            _options = options.Value;
        }

        public async Task SaveAsync(IEnumerable<VectorRecord> records)
        {
            var entitites = records.Select(record => new VectorRecordEntity
            {
                Id = record.Id,
                DocumentId = record.DocumentId,
                ChunkId = record.ChunkId,
                ChunkIndex = record.ChunkIndex,
                Content = record.Content,
                VectorJson = JsonSerializer.Serialize(record.Vector),
                FileName = record.FileName,
                CreatedOn = record.CreatedOn,
                ContentType = record.ContentType,
                UploadedAt = record.UploadedAt
            }).ToList();

            await _context.VectorRecords.AddRangeAsync(entitites);
            await _context.SaveChangesAsync();
        }

        public async Task<List<SearchResult>> SearchAsync(float[] queryVector, SearchFilter? filter, int topK, float similarityThreshold)
        {
            //var entities = await _context.VectorRecords.ToListAsync();
            var query = _context.VectorRecords.AsQueryable();
            if (filter is not null)
            {
                if (filter.DocumentId is Guid documentId)
                {
                    query = query.Where(x =>
                        x.DocumentId == documentId);
                }

                if (!string.IsNullOrWhiteSpace(filter.FileName))
                {
                    query = query.Where(x =>
                        x.FileName == filter.FileName);
                }

                if (!string.IsNullOrWhiteSpace(filter.ContentType))
                {
                    query = query.Where(x =>
                        x.ContentType == filter.ContentType);
                }

                if (filter.UploadedFrom is DateTime uploadedFrom)
                {
                    query = query.Where(x =>
                        x.UploadedAt >= uploadedFrom);
                }

                if (filter.UploadedTo is DateTime uploadedTo)
                {
                    query = query.Where(x =>
                        x.UploadedAt <= uploadedTo);
                }
            }
            var entities = await query.ToListAsync();
            var records = entities.Select(entity => new VectorRecord
            {
                Id = entity.Id,
                DocumentId = entity.DocumentId,
                ChunkId = entity.ChunkId,
                ChunkIndex = entity.ChunkIndex,
                Content = entity.Content,
                FileName = entity.FileName,
                Vector = JsonSerializer.Deserialize<float[]>(entity.VectorJson)!
            }).ToList();

            var result = records
        .Select(record => new SearchResult
        {
            Record = record,
            Similarity = VectorMath.CosineSimilarity(
                queryVector,
                record.Vector)
        })
        .Where(x => x.Similarity >= similarityThreshold)
        .OrderByDescending(x => x.Similarity)
        .Take(topK)
        .ToList();
            for (int i = 0; i < result.Count; i++)
            {
                result[i].Rank = i + 1;
            }
            return result;
        }
        public async Task<List<VectorRecord>> GetChunksAsync(
        Guid documentId,
        int startChunkIndex,
        int endChunkIndex)
        {
            var entities =
                await _context.VectorRecords
                    .Where(x =>
                        x.DocumentId == documentId &&
                        x.ChunkIndex >= startChunkIndex &&
                        x.ChunkIndex <= endChunkIndex)
                    .OrderBy(x => x.ChunkIndex)
                    .ToListAsync();

            return entities
                .Select(entity => new VectorRecord
                {
                    Id = entity.Id,
                    DocumentId = entity.DocumentId,
                    ChunkId = entity.ChunkId,
                    ChunkIndex = entity.ChunkIndex,
                    Content = entity.Content,
                    FileName = entity.FileName,
                    ContentType = entity.ContentType,
                    UploadedAt = entity.UploadedAt,
                    Vector = JsonSerializer.Deserialize<float[]>(
                        entity.VectorJson)!
                })
                .ToList();
        }
    }
}
