using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.AI.VectorStore;
using System.Text;
using System.Text.Json;

namespace AIChatAssistant.Services.AI.RAGPipeline
{
    public class ContextualCompressor : IContextualCompressor
    {
        private readonly IChatCompletionService _chatCompletionService;

        public ContextualCompressor(
            IChatCompletionService chatCompletionService)
        {
            _chatCompletionService = chatCompletionService;
        }

        public async Task<IReadOnlyList<SearchResult>> CompressAsync(
            string question,
            IReadOnlyList<SearchResult> results,
            CancellationToken cancellationToken = default)
        {
            // ==========================================
            // GUARD CLAUSES
            // ==========================================

            if (string.IsNullOrWhiteSpace(question) ||
                results.Count == 0)
            {
                return results;
            }

            // ==========================================
            // FILTER VALID CONTEXT
            // ==========================================

            var validResults =
                results
                    .Where(result =>
                        !string.IsNullOrWhiteSpace(
                            result.Record.Content))
                    .ToList();

            if (validResults.Count == 0)
            {
                return [];
            }
            if (ShouldSkipCompression(validResults))
            {
                Console.WriteLine(
                    "Contextual compression skipped: " +
                    "context is already small and highly relevant.");

                return validResults;
            }
            // ==========================================
            // BUILD BATCH PROMPT
            // ==========================================

            var systemPrompt =
                BuildSystemPrompt();

            var userPrompt =
                BuildUserPrompt(
                    question,
                    validResults);

            Console.WriteLine(
                "========= BATCH CONTEXT COMPRESSION =========");

            Console.WriteLine(
                $"Question: {question}");

            Console.WriteLine(
                $"Context count: {validResults.Count}");

            Console.WriteLine(
                "==============================================");

            // ==========================================
            // SINGLE LLM CALL
            // ==========================================

            var response =
                await _chatCompletionService
                    .CompleteStructuredAsync(
                        systemPrompt,
                        userPrompt,
                        cancellationToken);

            Console.WriteLine(
                "========= BATCH COMPRESSION RESPONSE =========");

            Console.WriteLine(response);

            Console.WriteLine(
                "==============================================");

            // ==========================================
            // CLEAN RESPONSE
            // ==========================================

            var content =
                CleanJsonResponse(response);

            BatchContextCompressionResult?
                compressionResult;

            try
            {
                compressionResult =
                    JsonSerializer.Deserialize<
                        BatchContextCompressionResult>(
                        content,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
            }
            catch (JsonException ex)
            {
                Console.WriteLine(
                    $"Batch compression JSON error: " +
                    $"{ex.Message}");

                // Important:
                // Do NOT destroy valid evidence if
                // the compression component fails.
                return validResults;
            }

            if (compressionResult is null ||
                compressionResult.Results.Count == 0)
            {
                Console.WriteLine(
                    "Batch compression returned no results.");

                return validResults;
            }

            // ==========================================
            // MAP ORIGINAL RESULTS BY RECORD ID
            // ==========================================

            var originalResults =
                validResults.ToDictionary(
                    result => result.Record.Id,
                    result => result);

            var compressedResults =
                new List<SearchResult>();

            foreach (var compressedItem
                     in compressionResult.Results)
            {
                if (!compressedItem.Relevant ||
                    string.IsNullOrWhiteSpace(
                        compressedItem.Content))
                {
                    continue;
                }

                if (!Guid.TryParse(
                      compressedItem.RecordId,
                      out var recordId))
                {
                    Console.WriteLine(
                        $"Invalid RecordId returned by compression: " +
                        $"{compressedItem.RecordId}");

                    continue;
                }

                if (!originalResults.TryGetValue(
                        recordId,
                        out var originalResult))
                {
                    Console.WriteLine(
                        $"Unknown RecordId returned by compression: " +
                        $"{compressedItem.RecordId}");

                    continue;
                }

                var compressedResult =
                    CreateCompressedResult(
                        originalResult,
                        compressedItem.Content);

                compressedResults.Add(
                    compressedResult);
            }

            // ==========================================
            // SAFE FALLBACK
            // ==========================================

            if (compressedResults.Count == 0)
            {
                Console.WriteLine(
                    "No valid compressed contexts were retained. " +
                    "Returning original contexts.");

                return validResults;
            }

            return compressedResults;
        }

        // ============================================================
        // SYSTEM PROMPT
        // ============================================================

        private static string BuildSystemPrompt()
        {
            return """
            You are a contextual compression component in a knowledge retrieval system.

            Your task is to identify and preserve information from each provided
            CONTEXT ITEM that is directly relevant to the USER QUESTION.

            The compressed content will be used as factual evidence by a later
            answer-generation and validation stage.

            ========================================
            STRICT EVIDENCE RULES
            ========================================

            1. Use ONLY information explicitly contained in each CONTEXT ITEM.

            2. Do NOT use pretrained knowledge.

            3. Do NOT use outside or general knowledge.

            4. Do NOT add new facts.

            5. Do NOT infer information that is not explicitly stated.

            6. Do NOT expand, explain, interpret, or enrich the context.

            7. Preserve the factual meaning of retained information.

            8. Preserve relevant information as close as possible to its original
            wording.

            9. Remove only information that is clearly unrelated to the USER QUESTION.

            10. If multiple statements in a CONTEXT ITEM are directly relevant,
            preserve all relevant statements.

            11. Do NOT combine information from different CONTEXT ITEMS.

            12. Evaluate every CONTEXT ITEM independently.

            ========================================
            RELEVANCE RULE
            ========================================

            For every CONTEXT ITEM:

            - Set relevant=true when it contains information that directly helps
            answer the USER QUESTION.

            - Set relevant=false when it does not contain relevant information.

            - When relevant=false, return an empty content value.

            ========================================
            OUTPUT RULES
            ========================================

            Return ONLY valid JSON.

            Return exactly one result for every CONTEXT ITEM provided.

            Preserve the RecordId exactly as provided.

            Do not invent RecordIds.

            Do not omit any RecordIds.

            Do not return markdown.

            Do not return explanations outside the JSON.

            Use this format:

            {
              "results": [
                {
                  "recordId": "original-record-id",
                  "relevant": true,
                  "content": "relevant information copied from the context"
                }
              ]
            }
            """;
        }

        // ============================================================
        // USER PROMPT
        // ============================================================

        private static string BuildUserPrompt(
            string question,
            IReadOnlyList<SearchResult> results)
        {
            var builder =
                new StringBuilder();

            builder.AppendLine(
                "USER QUESTION");

            builder.AppendLine(
                "========================================");

            builder.AppendLine(question);

            builder.AppendLine();

            builder.AppendLine(
                "CONTEXT ITEMS");

            builder.AppendLine(
                "========================================");

            foreach (var result in results)
            {
                builder.AppendLine(
                    $"RecordId: {result.Record.Id}");

                builder.AppendLine(
                    "Context:");

                builder.AppendLine(
                    result.Record.Content);

                builder.AppendLine(
                    "----------------------------------------");
            }

            return builder.ToString();
        }

        // ============================================================
        // RESULT MAPPING
        // ============================================================

        private static SearchResult CreateCompressedResult(
            SearchResult originalResult,
            string compressedContent)
        {
            return new SearchResult
            {
                Record = new VectorRecord
                {
                    Id = originalResult.Record.Id,
                    DocumentId =
                        originalResult.Record.DocumentId,

                    ChunkId =
                        originalResult.Record.ChunkId,

                    ChunkIndex =
                        originalResult.Record.ChunkIndex,

                    Content =
                        compressedContent.Trim(),

                    Vector =
                        originalResult.Record.Vector,

                    FileName =
                        originalResult.Record.FileName,

                    CreatedOn =
                        originalResult.Record.CreatedOn
                },

                Similarity =
                    originalResult.Similarity,

                RrfScore =
                    originalResult.RrfScore,

                RerankScore =
                    originalResult.RerankScore,

                Rank =
                    originalResult.Rank,

                QueryType =
                    originalResult.QueryType,

                QueryWeight =
                    originalResult.QueryWeight,

                SearchQuery =
                    originalResult.SearchQuery
            };
        }

        // ============================================================
        // JSON CLEANING
        // ============================================================

        private static string CleanJsonResponse(
            string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                return string.Empty;
            }

            var start =
                response.IndexOf('{');

            var end =
                response.LastIndexOf('}');

            if (start < 0 ||
                end < start)
            {
                return response.Trim();
            }

            return response[
                start..(end + 1)];
        }

        //=============================================================
        // Skip Compressional making optional
        //=============================================================
        private static bool ShouldSkipCompression(
    IReadOnlyList<SearchResult> results)
        {
            // Compression is unnecessary when there are only a few
            // highly relevant, reasonably small contexts.

            if (results.Count > 2)
            {
                return false;
            }

            // If any result has a weak rerank score,
            // compression may still be useful.
            if (results.Any(r => r.RerankScore < 0.80f))
            {
                return false;
            }

            // Avoid sending unnecessarily large contexts to the LLM.
            const int maxTotalCharacters = 4000;

            var totalCharacters =
                results.Sum(r =>
                    r.Record.Content?.Length ?? 0);

            return totalCharacters <= maxTotalCharacters;
        }
    }
}
