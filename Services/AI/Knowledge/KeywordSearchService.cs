using AIChatAssistant.Data;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.AI.VectorStore;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AIChatAssistant.Services.AI.Knowledge
{
    public class KeywordSearchService : IKeywordSearchService
    {
    private readonly AppDbContext _context;
    public KeywordSearchService(AppDbContext context)
    {
        _context = context;
    }
    public async Task<List<SearchResult>> SearchAsync(string query, SearchFilter? filter, int topK)
    {
            var recordsQuery =
          _context.VectorRecords.AsQueryable();

            if (filter is not null)
            {
                if (filter.DocumentId is Guid documentId)
                {
                    recordsQuery = recordsQuery.Where(
                        x => x.DocumentId == documentId);
                }

                if (!string.IsNullOrWhiteSpace(filter.FileName))
                {
                    recordsQuery = recordsQuery.Where(
                        x => x.FileName == filter.FileName);
                }

                if (!string.IsNullOrWhiteSpace(filter.ContentType))
                {
                    recordsQuery = recordsQuery.Where(
                        x => x.ContentType == filter.ContentType);
                }

                if (filter.UploadedFrom is DateTime uploadedFrom)
                {
                    recordsQuery = recordsQuery.Where(
                        x => x.UploadedAt >= uploadedFrom);
                }

                if (filter.UploadedTo is DateTime uploadedTo)
                {
                    recordsQuery = recordsQuery.Where(
                        x => x.UploadedAt <= uploadedTo);
                }
            }

            var records =
                await recordsQuery.ToListAsync();

            var keywords = query
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var results = records
                .Select(record =>
                {
                    var score = CalculateKeywordScore(
                        record.Content,
                        keywords);

                    return new SearchResult
                    {
                        Record = new VectorRecord
                        {
                            Id = record.Id,
                            DocumentId = record.DocumentId,
                            ChunkId = record.ChunkId,
                            ChunkIndex = record.ChunkIndex,
                            Content = record.Content,
                            FileName = record.FileName,
                            ContentType = record.ContentType,
                            UploadedAt = record.UploadedAt,
                            Vector = JsonSerializer.Deserialize<float[]>(
                                record.VectorJson)!
                        },
                        Similarity = score
                    };
                })
                .Where(x => x.Similarity > 0)
                .OrderByDescending(x => x.Similarity)
                .Take(topK)
                .ToList();

            for (var i = 0; i < results.Count; i++)
            {
                results[i].Rank = i + 1;
            }

            return results;
        }
        private static float CalculateKeywordScore(
        string content,
        IReadOnlyList<string> keywords)
        {
            if (string.IsNullOrWhiteSpace(content) ||
                keywords.Count == 0)
            {
                return 0;
            }

            var normalizedContent =
                content.ToLowerInvariant();

            var score = 0;

            foreach (var keyword in keywords)
            {
                var normalizedKeyword =
                    keyword.ToLowerInvariant();

                if (normalizedContent.Contains(
                        normalizedKeyword,
                        StringComparison.Ordinal))
                {
                    score++;
                }
            }

            return keywords.Count == 0 ? 0f : (float)score / keywords.Count;
        }
    }
}
