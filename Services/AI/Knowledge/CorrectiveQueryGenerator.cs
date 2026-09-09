using AIChatAssistant.Interfaces;

namespace AIChatAssistant.Services.AI.Knowledge
{
    public class CorrectiveQueryGenerator : ICorrectiveQueryGenerator
    {
        private readonly IChatCompletionService _chatCompletionService;

        public CorrectiveQueryGenerator(
            IChatCompletionService chatCompletionService)
        {
            _chatCompletionService = chatCompletionService;
        }

        public async Task<string> GenerateAsync(
            string question,
            string validationReason,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return string.Empty;
            }

            const string systemPrompt =
            """
            You are a corrective search query generator for an enterprise
            knowledge retrieval system.

            Your ONLY task is to produce ONE improved search query for retrieving
            relevant information from the knowledge base.

            Do NOT answer the user's question.

            Do NOT use pretrained knowledge.

            Do NOT use outside knowledge.

            Do NOT add facts, technologies, concepts, terminology, or entities
            that are not present in the USER QUESTION or the RETRIEVAL VALIDATION
            REASON.

            ========================================
            QUERY PRESERVATION
            ========================================

            1. Preserve the original user intent.

            2. Preserve important technical terms, names, acronyms, and entities
               from the USER QUESTION.

            3. Do not remove important terms merely to make the query shorter.

            4. If the original question is already a clear search query, make only
               minimal changes.

            5. Do not change the subject of the question.

            ========================================
            CORRECTION RULE
            ========================================

            Use the RETRIEVAL VALIDATION REASON only to improve retrieval precision.

            If the reason indicates that the retrieved context was unrelated,
            make the query more specific while preserving the original intent.

            If the reason indicates that the query was too broad, narrow the query
            using terms already present in the USER QUESTION.

            Do not invent terminology to compensate for missing knowledge.

            ========================================
            STRICT INFORMATION RULE
            ========================================

            The corrective query may contain only:

            - words or concepts from the USER QUESTION
            - words or concepts explicitly present in the RETRIEVAL VALIDATION REASON

            Do not introduce information from your own knowledge.

            ========================================
            OUTPUT RULE
            ========================================

            Return exactly ONE search query.

            Return ONLY the query text.

            Do not return:
            - explanations
            - reasoning
            - quotes
            - JSON
            - markdown
            - labels
            - prefixes such as "Query:"
            - multiple alternatives

            The output must be suitable for direct use as a knowledge-base
            search query.
            """;

            var input =
                $"""
            USER QUESTION:
            {question}

            RETRIEVAL VALIDATION REASON:
            {validationReason}
            """;


            var result =
                await _chatCompletionService.CompleteAsync(
                    systemPrompt,
                    input,
                    cancellationToken);

            return result.Trim();
        }
    }
}
