using AIChatAssistant.Configuration;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.AI.VectorStore;
using AIChatAssistant.Models.Quadrant;
using Azure.Core;
using Microsoft.Extensions.Options;
using System.Text.Json;
namespace AIChatAssistant.Services.Documents.VectorStores
{
    public class QdrantVectorStore : IVectorStore
    {
        private readonly HttpClient _httpClient;
        private readonly QdrantOptions _options;
        private static readonly JsonSerializerOptions QdrantJsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true  };
        public QdrantVectorStore(
        HttpClient httpClient,
        IOptions<QdrantOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task SaveAsync(IEnumerable<VectorRecord> records)
        {
            var points = records.Select(record => new QdrantPoint
            {
                Id = record.Id,
                Vector = record.Vector,
                Payload = new QdrantPayload
                {
                    DocumentId = record.DocumentId,
                    ChunkId = record.ChunkId,
                    ChunkIndex = record.ChunkIndex,
                    Content = record.Content,
                    FileName = record.FileName,
                    CreatedOn = record.CreatedOn,
                    ContentType = record.ContentType,
                    UploadedAt = record.UploadedAt
                }
            }).ToList();
            if (points.Count == 0)
                return;
            var request = new QdrantUpsertRequest
            {
                Points = points,
            };
            var url = $"{_options.BaseUrl}/collections/{_options.CollectionName}/points";

            var response = await _httpClient.PutAsJsonAsync(
                url,
                request);

            response.EnsureSuccessStatusCode();
        }

        public async Task<List<VectorRecord>> GetChunksAsync(
    Guid documentId,
    int startChunkIndex,
    int endChunkIndex)
        {
            var request = new QdrantScrollRequest
            {
                Filter = new QdrantFilter
                {
                    Must =
                    [
                        new QdrantCondition
                {
                    Key = "documentId",
                    Match = new QdrantMatch
                    {
                        Value = documentId
                    }
                },

                new QdrantCondition
                {
                    Key = "chunkIndex",
                    Range = new QdrantRange
                    {
                        Gte = startChunkIndex,
                        Lte = endChunkIndex
                    }
                }
                    ]
                },

                Limit = endChunkIndex - startChunkIndex + 1,

                WithPayload = true,

                WithVector = true
            };

            var url =
                $"{_options.BaseUrl}/collections/{_options.CollectionName}/points/scroll";

            var response = await _httpClient.PostAsJsonAsync(
                url,
                request);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<QdrantScrollResponse>();

            if (result?.Result?.Points == null)
                return [];

            return result.Result.Points
                .Select(MapToVectorRecord)
                .OrderBy(x => x.ChunkIndex)
                .ToList();
        }

        public async Task<List<SearchResult>> SearchAsync(
        float[] queryVector,
        SearchFilter? filter,
        int topK,
        float similarityThreshold)
        {
            var request = new QdrantQueryRequest
            {
                Query = queryVector,

                Limit = topK,

                WithPayload = true,

                WithVector = true
            };

            var url =
                $"{_options.BaseUrl}/collections/{_options.CollectionName}/points/query";

            var response = await _httpClient.PostAsJsonAsync(
                url,
                request,
                QdrantJsonOptions);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<QdrantQueryResponse>(QdrantJsonOptions);

            if (result?.Result?.Points == null)
                return [];

            return result.Result.Points
                 .Where(point => point.Score >= similarityThreshold)
                 .Select(point => new SearchResult
                 {
                     Record = MapToVectorRecord(
                     new QdrantPoint
                     {
                         Id = point.Id,
                         Vector = point.Vector,
                         Payload = point.Payload
                     }),

                     Similarity = point.Score
                 })
             .ToList();

            //var json = await response.Content.ReadAsStringAsync();

            //Console.WriteLine(json);

            //using var doc = JsonDocument.Parse(json);

            //var points = doc.RootElement
            //    .GetProperty("result")
            //    .GetProperty("points");

            //for (int i = 0; i < points.GetArrayLength(); i++)
            //{
            //    var documentId = points[i]
            //        .GetProperty("payload")
            //        .GetProperty("documentId")
            //        .GetString();

            //    Console.WriteLine($"Point {i} DocumentId: {documentId}");

            //    Console.WriteLine(
            //        Guid.TryParse(documentId, out var guid)
            //            ? $"Valid GUID: {guid}"
            //            : "INVALID GUID");
            //}

            //return new List<SearchResult>();

        }
        private static QdrantFilter? MapFilter(SearchFilter? filter)
        {
            if (filter == null)
                return null;

            var conditions = new List<QdrantCondition>();

            if (filter.DocumentId.HasValue)
            {
                conditions.Add(new QdrantCondition
                {
                    Key = "documentId",
                    Match = new QdrantMatch
                    {
                        Value = filter.DocumentId.Value
                    }
                });
            }

            if (!string.IsNullOrWhiteSpace(filter.FileName))
            {
                conditions.Add(new QdrantCondition
                {
                    Key = "fileName",
                    Match = new QdrantMatch
                    {
                        Value = filter.FileName
                    }
                });
            }

            if (!string.IsNullOrWhiteSpace(filter.ContentType))
            {
                conditions.Add(new QdrantCondition
                {
                    Key = "contentType",
                    Match = new QdrantMatch
                    {
                        Value = filter.ContentType
                    }
                });
            }

            if (conditions.Count == 0)
                return null;

            return new QdrantFilter
            {
                Must = conditions
            };
        }
        private static VectorRecord MapToVectorRecord(
  QdrantPoint point)
        {
            return new VectorRecord
            {
                Id = point.Id,

                Vector = point.Vector,

                DocumentId = point.Payload.DocumentId,

                ChunkId = point.Payload.ChunkId,

                ChunkIndex = point.Payload.ChunkIndex,

                Content = point.Payload.Content,

                FileName = point.Payload.FileName,

                CreatedOn = point.Payload.CreatedOn,

                ContentType = point.Payload.ContentType,

                UploadedAt = point.Payload.UploadedAt
            };
        }
    }
}
