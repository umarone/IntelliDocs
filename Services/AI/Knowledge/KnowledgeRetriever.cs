using AIChatAssistant.Configuration;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.AI.VectorStore;
using AIChatAssistant.Services.AI.RAGPipeline;
using Microsoft.Extensions.Options;

namespace AIChatAssistant.Services.AI.Knowledge
{
    public class KnowledgeRetriever : IKnowledgeRetriever
    {
        private readonly IQueryDecomposer _queryDecomposer;
        private readonly IMultiQueryGenerator _multiQueryGenerator;
        private readonly IHybridRetriever _hybridRetriever;
        private readonly RagOptions _options;
        private readonly IVectorStore _vectorStore;
        private readonly IContextualCompressor _contextualCompressor;
        private readonly IRetrievalValidator _retrievalValidator;
        private readonly ICorrectiveQueryGenerator _correctiveQueryGenerator;
        private readonly AdaptiveRetrievalStrategy _adaptiveRetrievalStrategy;
        private readonly IMmrSelector _mmrSelector;
        private readonly RetrievalConfidenceCalculator _retrievalConfidenceCalculator;
        public KnowledgeRetriever(
            IOptions<RagOptions> options,
            IMultiQueryGenerator multiQueryGenerator,
            IHybridRetriever hybridRetriever,
            IQueryDecomposer queryDecomposer, IVectorStore vectorStore, IContextualCompressor contextualCompressor,
            IRetrievalValidator retrievalValidator, ICorrectiveQueryGenerator correctiveQueryGenerator, 
            AdaptiveRetrievalStrategy adaptiveRetrievalStrategy, IMmrSelector mmrSelector, RetrievalConfidenceCalculator retrievalConfidenceCalculator)
        {
            _options = options.Value;
            _multiQueryGenerator = multiQueryGenerator;
            _hybridRetriever = hybridRetriever;
            _queryDecomposer = queryDecomposer;
            _vectorStore = vectorStore;
            _contextualCompressor = contextualCompressor;
            _retrievalValidator = retrievalValidator;
            _correctiveQueryGenerator = correctiveQueryGenerator;
            _adaptiveRetrievalStrategy = adaptiveRetrievalStrategy;
            _mmrSelector = mmrSelector;
            _retrievalConfidenceCalculator = retrievalConfidenceCalculator;
        }
        public async Task<IReadOnlyList<SearchResult>> RetrieveAsync(
    string question, string originalQuestion,
    SearchFilter? filter = null)
        {

            if (string.IsNullOrWhiteSpace(question))
            {
                return [];
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var correctiveAttempts = 0;

            Console.WriteLine(
                $"Adaptive Retrieval Original Question: " +
                $"'{originalQuestion}'");

            Console.WriteLine(
                $"Adaptive Retrieval Search Query: " +
                $"'{question}'");
            var adaptiveStrategy = await _adaptiveRetrievalStrategy.GetStrategyAsync(originalQuestion);
            // ==========================================
            // INITIAL RETRIEVAL
            // ==========================================

            var topK = adaptiveStrategy.TopK;
            var similarityThreshold = adaptiveStrategy.Threshold;

            var (
                results,
                validation) =
                    await RetrieveAndValidateAsync(
                        question,
                        originalQuestion,
                        topK,
                        similarityThreshold,
                        filter,
                         "Initial");


            // ==========================================
            // ACCEPT GOOD RETRIEVAL
            // ==========================================

            if (validation.IsRelevant && validation.Confidence >=
                _options.MinimumRetrievalConfidence &&
                validation.RetrievalConfidence >=
                _options.MinimumRetrievalConfidence)
            {
                stopwatch.Stop();

                LogRetrievalSummary(
                topK,
                similarityThreshold,
                initialAccepted: true,
                correctiveAttempts,
                results,
                validation,
                stopwatch.ElapsedMilliseconds,
                "Initial");

                return results;
            }
            // ==========================================
            // CORRECTIVE RETRIEVAL
            // ==========================================

            var correctiveQuestion = question;

            for (var attempt = 1;
                 attempt <= _options.MaxCorrectiveRetrievalAttempts;
                 attempt++)
            {
                correctiveAttempts++;
                Console.WriteLine(
                    "========= CORRECTIVE RETRIEVAL =========");

                Console.WriteLine(
                    $"Attempt: {attempt}/" +
                    $"{_options.MaxCorrectiveRetrievalAttempts}");

                Console.WriteLine(
                    $"Previous relevance: " +
                    $"{validation.IsRelevant}");

                Console.WriteLine(
                    $"Previous confidence: " +
                    $"{validation.Confidence:F3}");

                Console.WriteLine(
                    $"Previous reason: " +
                    $"{validation.Reason}");

                // ==========================================
                // GENERATE CORRECTIVE QUERY
                // ==========================================

                correctiveQuestion = await _correctiveQueryGenerator.GenerateAsync(
                                    correctiveQuestion,
                                    validation.Reason);

                correctiveQuestion =
                    correctiveQuestion
                        .Trim()
                        .Trim('`', '"', '\'');

                if (string.IsNullOrWhiteSpace(correctiveQuestion))
                {
                    Console.WriteLine(
                        "Corrective query generation returned an empty query.");

                    break;
                }

                Console.WriteLine(
                    $"Corrective Query: {correctiveQuestion}");

                Console.WriteLine(
                    "========================================");

                // ==========================================
                // RETRY RETRIEVAL
                // ==========================================

                (
                    results,
                    validation) =
                        await RetrieveAndValidateAsync(
                            correctiveQuestion,
                            originalQuestion,
                            topK,
                            similarityThreshold,
                            filter,
                            "Corrective");

                // ==========================================
                // ACCEPT CORRECTIVE RETRIEVAL
                // ==========================================

                if (validation.IsRelevant &&
                validation.Confidence >=
                _options.MinimumRetrievalConfidence &&
                validation.RetrievalConfidence >=
                _options.MinimumRetrievalConfidence)
                {
                    stopwatch.Stop();

                    LogRetrievalSummary(
                    topK,
                    similarityThreshold,
                    initialAccepted: false,
                    correctiveAttempts,
                    results,
                    validation,
                    stopwatch.ElapsedMilliseconds,
                    "Corrective");

                    return results;
                }
            }

            // ==========================================
            // RETRIEVAL FAILED
            // ==========================================
            stopwatch.Stop();

            LogRetrievalSummary(
            topK,
            similarityThreshold,
            initialAccepted: false,
            correctiveAttempts,
            results,
            validation,
            stopwatch.ElapsedMilliseconds,
            "Failed");

            return [];
        }
        private async Task<IReadOnlyList<SearchResult>> ExpandContextAsync(
    IReadOnlyList<SearchResult> results)
        {
            if (results.Count == 0 ||
                _options.ContextWindow <= 0)
            {
                return results;
            }

            var expandedResults = new List<SearchResult>();

            foreach (var result in results)
            {
                var record = result.Record;

                var startIndex =
                    Math.Max(
                        0,
                        record.ChunkIndex - _options.ContextWindow);

                var endIndex =
                    record.ChunkIndex + _options.ContextWindow;

                var contextChunks =
                    await _vectorStore.GetChunksAsync(
                        record.DocumentId,
                        startIndex,
                        endIndex);

                // DEBUG: inspect context chunks

                Console.WriteLine(
                $"EXPANDING RESULT: " +
                $"DocumentId={record.DocumentId}, " +
                $"ChunkIndex={record.ChunkIndex}, " +
                $"Similarity={result.Similarity:F3}");

                Console.WriteLine(
                    $"Original Content: {record.Content}");
                Console.WriteLine(
                    $"DocumentId={record.DocumentId}, " +
                    $"Requested range={startIndex}-{endIndex}");

                foreach (var chunk in contextChunks)
                {
                    Console.WriteLine(
                        $"Loaded ChunkIndex={chunk.ChunkIndex}");

                    Console.WriteLine(chunk.Content);
                    Console.WriteLine("------------------------------------");
                }
                if (contextChunks.Count == 0)
                {
                    expandedResults.Add(result);
                    continue;
                }

                var contextContent =
                    string.Join(
                        Environment.NewLine + Environment.NewLine,
                        contextChunks
                            .OrderBy(x => x.ChunkIndex)
                            .Select(x => x.Content));

                var expandedRecord = new VectorRecord
                {
                    Id = record.Id,
                    DocumentId = record.DocumentId,
                    ChunkId = record.ChunkId,
                    ChunkIndex = record.ChunkIndex,
                    Content = contextContent,
                    Vector = record.Vector,
                    FileName = record.FileName,
                    CreatedOn = record.CreatedOn
                };

                expandedResults.Add(
                    new SearchResult
                    {
                        Record = expandedRecord,
                        Similarity = result.Similarity,
                        RrfScore = result.RrfScore,
                        RerankScore = result.RerankScore,
                        Rank = result.Rank
                    });
            }
            Console.WriteLine("========= CONTEXT EXPANSION =========");

            foreach (var result in expandedResults)
            {
                Console.WriteLine(
                    $"Rank={result.Rank}, ChunkIndex={result.Record.ChunkIndex}");

                Console.WriteLine(result.Record.Content);
                Console.WriteLine("------------------------------------");
            }

            Console.WriteLine("====================================");

            return expandedResults;
        }
        private async Task<(IReadOnlyList<SearchResult> Results,RetrievalValidationResult Validation)> RetrieveAndValidateAsync(
      string searchQuery,
      string OriginalQuestion,
      int topK,
      float similarityThreshold,
      SearchFilter? filter = null,
      string retrievalType = "Initial")
        {
            // ==========================================
            // STEP 1: Query decomposition
            // ==========================================

            //var decomposedQueries =
            //    await _queryDecomposer.DecomposeAsync(
            //        OriginalQuestion);

            var decomposedQueries =
                await _queryDecomposer.DecomposeIfNeededAsync(
                    OriginalQuestion);

            Console.WriteLine(
                "========= QUERY DECOMPOSITION =========");

            Console.WriteLine(
                $"Original Question: {OriginalQuestion}");

            foreach (var query in decomposedQueries)
            {
                Console.WriteLine(
                    $"Sub-question: {query}");
            }

            Console.WriteLine(
                "=======================================");

            // ==========================================
            // STEP 2: Multi-query generation
            // ==========================================

            var searchQueries =
                new List<string>();
            if (decomposedQueries.Count == 1)
            {
                searchQueries.Add(decomposedQueries[0]);

                Console.WriteLine(
                    "Multi-query generation skipped: only one decomposed query.");
            }
            else
            {

                foreach (var decomposedQuery in decomposedQueries)
                {
                    var generatedQueries =
                        await _multiQueryGenerator.GenerateAsync(
                            decomposedQuery);

                    searchQueries.AddRange(
                        generatedQueries);
                }
            }

            // Always preserve the actual search query.
            // This is important for corrective retrieval.
            searchQueries.Add(searchQuery);

            searchQueries = searchQueries
            .Where(q =>
            !string.IsNullOrWhiteSpace(q))
            .Select(q => q.Trim())
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .Take(9)
            .ToList();

            if (!string.IsNullOrWhiteSpace(searchQuery) &&
                !searchQueries.Contains(
                    searchQuery,
                    StringComparer.OrdinalIgnoreCase))
            {
                searchQueries.Add(searchQuery.Trim());
            }

            Console.WriteLine(
                "========= MULTI QUERY RETRIEVAL =========");

            Console.WriteLine(
                $"Original Search Query: {searchQuery}");

            foreach (var query in searchQueries)
            {
                Console.WriteLine(
                    $"Search Query: {query}");
            }

            Console.WriteLine(
                $"Total Search Queries: {searchQueries.Count}");

            Console.WriteLine(
                "==========================================");

            // ==========================================
            // STEP 3: Retrieve candidates
            // ==========================================

            var allCandidates =
                new List<SearchResult>();

            foreach (var query in searchQueries)
            {
                var queryType =
                    string.Equals(
                        query,
                        searchQuery,
                        StringComparison.OrdinalIgnoreCase)
                        ? retrievalType
                        : "Generated";

                var queryWeight = queryType switch
                {
                   "Initial" => 1.0f,
                   "Corrective" => 1.0f,
                   "Generated" => 0.8f,
                   _ => 0.8f
                };

                var candidates =
                    await _hybridRetriever.RetrieveCandidatesAsync(
                        query,
                        filter,
                        topK * 2,
                        similarityThreshold,
                        queryType);
                foreach (var candidate in candidates)
                {
                    candidate.QueryWeight = queryWeight;
                }

                allCandidates.AddRange(
                    candidates);
            }

            Console.WriteLine(
                $"Total candidates before merge: " +
                $"{allCandidates.Count}");

            // ==========================================
            // STEP 4: Remove duplicate chunks
            // ==========================================

            var uniqueCandidates =
                allCandidates
                    .GroupBy(x => x.Record.Id)
                    .Select(group =>
                        group
                            .OrderByDescending(x => x.RrfScore)
                            .First())
                    .ToList();

            Console.WriteLine(
                $"Unique candidates after merge: " +
                $"{uniqueCandidates.Count}");

            // ==========================================
            // STEP 5: Reranking
            // ==========================================

            var rerankedResults =
                await _hybridRetriever.RerankAsync(
                    OriginalQuestion,
                    uniqueCandidates,
                    topK);

            Console.WriteLine(
                $"Final reranked results: " +
                $"{rerankedResults.Count}");


            // ==========================================
            // STEP 6: MMR
            // ==========================================

            var finalResults = rerankedResults;

            if (_options.EnableMmr)
            {
                var mmrCandidates =
                    rerankedResults
                        .Where(x =>
                            x.RerankScore >= _options.RerankThreshold)
                        .ToList();

                Console.WriteLine(
                    "========= MMR RELEVANCE FILTER =========");

                Console.WriteLine(
                    $"Candidates before filter: " +
                    $"{rerankedResults.Count}");

                Console.WriteLine(
                    $"Candidates after filter: " +
                    $"{mmrCandidates.Count}");

                Console.WriteLine(
                    $"Rerank Threshold: " +
                    $"{_options.RerankThreshold:F2}");

                Console.WriteLine(
                    "=========================================");

                finalResults =
                    _mmrSelector
                        .Select(
                            mmrCandidates,
                            topK,
                            _options.MmrLambda)
                        .ToList();

                Console.WriteLine(
                    "========= MMR SELECTION =========");

                Console.WriteLine(
                    $"Candidates before MMR: " +
                    $"{mmrCandidates.Count}");

                Console.WriteLine(
                    $"Results after MMR: " +
                    $"{finalResults.Count}");

                Console.WriteLine(
                    $"MMR Lambda: " +
                    $"{_options.MmrLambda:F2}");

                Console.WriteLine(
                    "=================================");
            }

            Console.WriteLine(
                "========= BEFORE CONTEXT EXPANSION =========");

            foreach (var result in rerankedResults)
            {
                Console.WriteLine(
                $"Rank={result.Rank}, " +
                $"QueryType={result.QueryType}, " +
                $"QueryWeight={result.QueryWeight:F2}, " +
                $"SearchQuery={result.SearchQuery}, " +
                $"RecordId={result.Record.Id}, " +
                $"ChunkId={result.Record.ChunkId}, " +
                $"DocumentId={result.Record.DocumentId}, " +
                $"ChunkIndex={result.Record.ChunkIndex}, " +
                $"Similarity={result.Similarity:F3}, " +
                $"RrfScore={result.RrfScore:F4}, " +
                $"RerankScore={result.RerankScore:F3}");

                Console.WriteLine(
                    result.Record.Content);

                Console.WriteLine(
                    "------------------------------------");
            }

            Console.WriteLine(
                "============================================");

            // ==========================================
            // STEP 6: Context expansion
            // ==========================================

            var contextExpandedResults = await ExpandContextAsync(finalResults);

            Console.WriteLine(
                $"Context-expanded results: " +
                $"{contextExpandedResults.Count}");

            // ==========================================
            // STEP 7: Contextual compression
            // ==========================================

            var compressedResults =
                await _contextualCompressor.CompressAsync(
                    OriginalQuestion,
                    contextExpandedResults);

            Console.WriteLine(
                $"Context-compressed results: " +
                $"{compressedResults.Count}");

            // ==========================================
            // STEP 8: Retrieval validation
            // ==========================================

            RetrievalValidationResult validation;

            if (compressedResults.Count > 0 &&
                compressedResults.All(x =>
                    x.RerankScore >= _options.RerankThreshold))
            {
                validation = new RetrievalValidationResult
                {
                    IsRelevant = true,
                    Confidence = 1.0f,
                    Reason =
                        "Retrieval passed the configured rerank threshold."
                };

                Console.WriteLine(
                    "Retrieval validation skipped: " +
                    "all results passed the rerank threshold.");
            }
            else
            {
                validation =
                    await _retrievalValidator.ValidateAsync(
                        OriginalQuestion,
                        compressedResults);
            }

            Console.WriteLine(
                "========= RETRIEVAL VALIDATION =========");

            Console.WriteLine(
                $"Relevant = {validation.IsRelevant}");

            Console.WriteLine(
                $"Confidence = {validation.Confidence:F3}");

            Console.WriteLine(
                $"Reason = {validation.Reason}");

            Console.WriteLine(
                "========================================");

            // ==========================================
            // STEP 8: confidence Calculator
            // ==========================================


            var retrievalConfidence = _retrievalConfidenceCalculator.Calculate(compressedResults, validation);

            validation.RetrievalConfidence = retrievalConfidence;

            Console.WriteLine(
                "========= RETRIEVAL CONFIDENCE =========");

            Console.WriteLine(
                $"Validator Confidence: " +
                $"{validation.Confidence:F3}");

            Console.WriteLine(
                $"Retrieval Confidence: " +
                $"{retrievalConfidence:F3}");

            Console.WriteLine(
                "========================================");

            return (
                compressedResults,
                validation);
        }

        private void LogRetrievalSummary(
        int topK,
        float similarityThreshold,
        bool initialAccepted,
        int correctiveAttempts,
        IReadOnlyList<SearchResult> results,
        RetrievalValidationResult validation,
        long elapsedMilliseconds,
        string finalRetrieval)
        {
            Console.WriteLine(
                "========= RETRIEVAL SUMMARY =========");

            Console.WriteLine(
                $"Strategy TopK: {topK}");

            Console.WriteLine(
                $"Strategy Threshold: {similarityThreshold:F3}");

            Console.WriteLine(
                $"Initial Retrieval: {(initialAccepted ? "Accepted" : "Rejected")}");

            Console.WriteLine(
                $"Corrective Attempts: {correctiveAttempts}");

            Console.WriteLine(
                $"Final Retrieval: {finalRetrieval}");

            Console.WriteLine(
                $"Final Results: {results.Count}");

            Console.WriteLine(
                $"Validation Confidence: {validation.Confidence:F3}");

            Console.WriteLine(
                $"Retrieval Confidence: " + $"{validation.RetrievalConfidence:F3}");

            Console.WriteLine(
                $"Total Retrieval Time: {elapsedMilliseconds} ms");

            Console.WriteLine(
                $"Final Validation Reason: {validation.Reason}");

            Console.WriteLine(
                "=====================================");
        }
    }

}
