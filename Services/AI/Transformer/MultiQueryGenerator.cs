using AIChatAssistant.Interfaces;

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

            Generate exactly three concise search queries representing the user's
            original question from different search perspectives.

            Rules:
            1. Preserve the original intent of the user's question.
            2. Preserve important terms, entities, and concepts from the original question.
            3. Rephrase the question using different wording or search perspectives
               without changing its information need.
            4. Do not introduce new concepts, entities, technologies, products,
               organizations, locations, or topics that are not present in the
               original question.
            5. Do not answer the question.
            6. Return exactly three queries.
            7. Return one query per line.
            8. Do not number the queries.
            9. Do not add explanations or commentary.
            10. For short or highly specific questions, keep the generated queries
                tightly focused on the same information need and terminology.
            """;

            var result =
                await _chatCompletionService.CompleteAsync(
                    systemPrompt,
                    question,
                    cancellationToken);

            var queries =
                result
                    .Split(
                        '\n',
                        StringSplitOptions.RemoveEmptyEntries |
                        StringSplitOptions.TrimEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3)
                    .ToList();

            return queries;
        }
    }
}
