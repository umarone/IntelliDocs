using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.RAG;
using System.Text.Json;

namespace AIChatAssistant.Services.AI.Transformer
{
    public class MultiQueryGenerator : IMultiQueryGenerator
    {
        private readonly IChatCompletionService _chatCompletionService;

        public MultiQueryGenerator(
            IChatCompletionService chatCompletionService)
        {
            _chatCompletionService = chatCompletionService;
        }

        public async Task<IReadOnlyList<string>> GenerateAsync(
            string question,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return [];
            }

            const string systemPrompt =
            """
        You are a search query generator for a knowledge retrieval system.

        Generate exactly three concise search queries representing the same
        information need as the user's original question.

        Use different wording only.

        Rules:

        1. Preserve the original information need.

        2. Preserve important terms, entities, and concepts from the
           original question.

        3. Rephrase the original question using different wording only.
           Do not change the requested perspective, scope, or intent.

        4. Do not introduce any new entity, concept, technology, product,
           organization, location, topic, relationship, or information need
           that is not present in the original question.

        5. Do not answer the question.

        6. Do not expand the scope of the question.

        7. Do not infer additional information needs.

        8. For short or highly specific questions, keep all generated
           queries tightly focused on the same terminology and information need.

        9. Return exactly three queries.

        10. Return ONLY valid JSON using this exact structure:

        {
          "queries": [
            "query 1",
            "query 2",
            "query 3"
          ]
        }
        """;

            var response =
                await _chatCompletionService.CompleteStructuredAsync(
                    systemPrompt,
                    question,
                    cancellationToken);

            Console.WriteLine(
                "========= MULTI-QUERY GENERATION RESPONSE =========");

            Console.WriteLine(response);

            Console.WriteLine(
                "====================================================");

            MultiQueryGenerationResult? result;

            try
            {
                result =
                    JsonSerializer.Deserialize<MultiQueryGenerationResult>(
                        response,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
            }
            catch (JsonException ex)
            {
                Console.WriteLine(
                    $"Multi-query JSON error: {ex.Message}");

                return [question];
            }

            if (result is null ||
                result.Queries is null ||
                result.Queries.Count == 0)
            {
                Console.WriteLine(
                    "Multi-query generation returned no queries.");

                return [question];
            }

            var queries =
                result.Queries
                    .Where(q => !string.IsNullOrWhiteSpace(q))
                    .Select(q => q.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3)
                    .ToList();

            if (queries.Count == 0)
            {
                return [question];
            }

            Console.WriteLine(
                "========= GENERATED SEARCH QUERIES =========");

            foreach (var query in queries)
            {
                Console.WriteLine(query);
            }

            Console.WriteLine(
                "=============================================");

            return queries;
        }
    }
}
