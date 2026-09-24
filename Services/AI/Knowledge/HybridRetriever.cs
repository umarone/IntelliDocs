using AIChatAssistant.Configuration;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.AI.VectorStore;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace AIChatAssistant.Services.AI.Knowledge
{
    public class HybridRetriever : IHybridRetriever
    {
        private readonly IEmbeddingProvider _embeddingProvider;
        private readonly IVectorStore _vectorStore;
        private readonly IKeywordSearchService _keywordSearchService;
        private readonly IReranker _reranker;
        private readonly RagOptions _options;

        public HybridRetriever(
            IEmbeddingProvider embeddingProvider,
            IVectorStore vectorStore,
            IKeywordSearchService keywordSearchService,
            IReranker reranker,
            IOptions<RagOptions> options)
        {
            _embeddingProvider = embeddingProvider;
            _vectorStore = vectorStore;
            _keywordSearchService = keywordSearchService;
            _reranker = reranker;
            _options = options.Value;
        }

        public async Task<List<SearchResult>> SearchAsync(
    string searchQuery,
    string originalQuery,
    SearchFilter? filter = null,
    int topK = 5)
        {
            var candidates =
                await RetrieveCandidatesAsync(
                    searchQuery,
                    filter,
                    topK * 2);

            Console.WriteLine(
                $"Hybrid candidates: {candidates.Count}");

            return await RerankAsync(
                originalQuery,
                candidates,
                topK);
        }

        public async Task<List<SearchResult>> RetrieveCandidatesAsync(
         string searchQuery,
         SearchFilter? filter = null,
         int topK = 10,
         float similarityThreshold = 0.5f,
         string queryType = "Unknown")
        {
            var embedingstart = Stopwatch.GetTimestamp();
            var embedding =
                await _embeddingProvider
                    .GenerateEmbeddingAsync(searchQuery);

            var embedingelapsed = Stopwatch.GetElapsedTime(embedingstart);
            Console.WriteLine(
    $"Embedding generation time: " +
    $"{embedingelapsed.TotalMilliseconds:F0} ms");

            var vectorstart = Stopwatch.GetTimestamp();

            var vectorResults =
                await _vectorStore.SearchAsync(
                    embedding,
                    filter,
                    topK,
                    similarityThreshold);
            var vectorElapsed = Stopwatch.GetElapsedTime(vectorstart);
            Console.WriteLine(
    $"Vector search time: " +
    $"{vectorElapsed.TotalMilliseconds:F0} ms");

            //foreach (var item in vectorResults)
            //{
            //    // Print all float values joined by commas
            //    Console.WriteLine($"vectors are [{string.Join(", ", item.Record.Vector)}]");
            //}

            Console.WriteLine(
                $"Vector results: {vectorResults.Count}");
            var keywordStart = Stopwatch.GetTimestamp();

            var keywordResults =
                await _keywordSearchService.SearchAsync(
                    searchQuery,
                    filter,
                    topK);

            var keywordElapsed = Stopwatch.GetElapsedTime(keywordStart);
            Console.WriteLine(
    $"Keyword search time: " +
    $"{keywordElapsed.TotalMilliseconds:F0} ms");

            Console.WriteLine(
                $"Keyword results: {keywordResults.Count}");

            var hybridResults =
                FuseResults(
                    vectorResults,
                    keywordResults,
                    topK);
            foreach (var result in hybridResults)
            {
                result.SearchQuery = searchQuery;
                result.QueryType = queryType;
            }

            return hybridResults;
        }

        public async Task<List<SearchResult>> RetrieveCandidatesAsync(
    IReadOnlyList<string> searchQueries,
    SearchFilter? filter = null,
    int topK = 10,
    float similarityThreshold = 0.5f,
    string queryType = "Unknown")
        {
            if (searchQueries == null || searchQueries.Count == 0)
            {
                return [];
            }

            var embeddingStart = Stopwatch.GetTimestamp();

            var embeddings =
                await _embeddingProvider.GenerateEmbeddingsAsync(
                    searchQueries);

            var embeddingElapsed =
                Stopwatch.GetElapsedTime(embeddingStart);

            Console.WriteLine(
                $"Batch embedding generation time: " +
                $"{embeddingElapsed.TotalMilliseconds:F0} ms");

            if (embeddings.Count != searchQueries.Count)
            {
                throw new InvalidOperationException(
                    "The number of embeddings returned does not match the number of search queries.");
            }

            var allCandidates =
                new List<SearchResult>();

            for (int i = 0; i < searchQueries.Count; i++)
            {
                var query = searchQueries[i];
                var embedding = embeddings[i];

                var vectorStart = Stopwatch.GetTimestamp();

                var vectorResults =
                    await _vectorStore.SearchAsync(
                        embedding,
                        filter,
                        topK,
                        similarityThreshold);

                var vectorElapsed =
                    Stopwatch.GetElapsedTime(vectorStart);

                Console.WriteLine(
                    $"Vector search [{i + 1}] time: " +
                    $"{vectorElapsed.TotalMilliseconds:F0} ms");

                var keywordStart = Stopwatch.GetTimestamp();

                var keywordResults =
                    await _keywordSearchService.SearchAsync(
                        query,
                        filter,
                        topK);

                var keywordElapsed =
                    Stopwatch.GetElapsedTime(keywordStart);

                Console.WriteLine(
                    $"Keyword search [{i + 1}] time: " +
                    $"{keywordElapsed.TotalMilliseconds:F0} ms");

                var hybridResults =
                    FuseResults(
                        vectorResults,
                        keywordResults,
                        topK);

                foreach (var result in hybridResults)
                {
                    result.SearchQuery = query;
                    result.QueryType = queryType;
                }

                allCandidates.AddRange(
                    hybridResults);
            }

            return allCandidates;
        }

        public async Task<List<SearchResult>> RerankAsync(
        string originalQuery,
        IReadOnlyList<SearchResult> candidates,
        int topK)
        {
            var rerankedResults =
                await _reranker.RerankAsync(
                    originalQuery,
                    candidates,
                    topK);

            Console.WriteLine(
                $"Reranked results: {rerankedResults.Count}");

            LogResults(rerankedResults);

            return rerankedResults;
        }
        private List<SearchResult> FuseResults(
            IReadOnlyList<SearchResult> vectorResults,
            IReadOnlyList<SearchResult> keywordResults,
            int topK)
        {
            
            var scores =
                new Dictionary<Guid, float>();

            var results =
                new Dictionary<Guid, SearchResult>();

            AddResults(
                vectorResults,
                scores,
                results,
                _options.RrfK);

            AddResults(
                keywordResults,
                scores,
                results,
                _options.RrfK);

            return scores
                .OrderByDescending(x => x.Value)
                .Take(topK)
                .Select((x, index) =>
                {
                    var result = results[x.Key];

                    result.RrfScore = x.Value;
                    result.Rank = index + 1;

                    return result;
                })
                .ToList();
        }

        private static void AddResults(
     IReadOnlyList<SearchResult> searchResults,
     Dictionary<Guid, float> scores,
     Dictionary<Guid, SearchResult> results,
     float k)
        {
            foreach (var result in searchResults)
            {
                var id = result.Record.Id;

                if (!scores.ContainsKey(id))
                {
                    scores[id] = 0;
                    results[id] = result;
                }

                var rrfContribution =
                    1f / (k + result.Rank);

                scores[id] +=
                    rrfContribution * result.QueryWeight;
            }
        }

        private static void LogResults(
            IReadOnlyList<SearchResult> results)
        {
            foreach (var result in results)
            {
                Console.WriteLine(
                    $"Rank={result.Rank}, " +
                    $"Similarity={result.Similarity:F3}, " +
                    $"RrfScore={result.RrfScore:F4}, " +
                    $"RerankScore={result.RerankScore:F3}");

                Console.WriteLine(
                    $"Content: {result.Record.Content}");

                Console.WriteLine(
                    "----------------------------------------");
            }
        }
    }
}
