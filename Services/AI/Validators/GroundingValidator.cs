using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Ollama;
using AIChatAssistant.Models.Validators;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace AIChatAssistant.Services.AI.Validators
{
    public class GroundingValidator : IGroundingValidator
    {
        private readonly IOllamaClient _ollamaClient;

        public GroundingValidator(
            IOllamaClient ollamaClient)
        {
            _ollamaClient = ollamaClient;
        }
        public async Task<GroundingValidationResult> ValidateAsync(
        string question,
        string answer,
        IReadOnlyList<string> selectedEvidence)
        {
            var directCheckStart = Stopwatch.GetTimestamp();

            var directlySupported =
                IsDirectlySupported(
                    answer,
                    selectedEvidence);

            var directCheckElapsed =
                Stopwatch.GetElapsedTime(directCheckStart);

            Console.WriteLine(
                $"Grounding deterministic check: " +
                $"{directCheckElapsed.TotalMilliseconds:F0} ms");

            if (directlySupported)
            {
                Console.WriteLine(
                    "Grounding validation skipped: " +
                    "answer is directly supported by the selected evidence.");

                return new GroundingValidationResult
                {
                    Grounded = true,
                    Reason =
                        "The generated answer is directly supported by the selected evidence."
                };
            }

            Console.WriteLine(
                "========= SELECTED EVIDENCE BEFORE KNOWLEDGE BASE =========");

            foreach (var evidence in selectedEvidence)
            {
                Console.WriteLine("----- EVIDENCE -----");
                Console.WriteLine(evidence);
            }

            var knowledgeBase =
                selectedEvidence.Count == 0
                    ? "NO KNOWLEDGE AVAILABLE"
                    : string.Join(
                        "\n",
                        selectedEvidence
                            .Where(e => !string.IsNullOrWhiteSpace(e)));

            var systemPrompt = """
            You are a strict factual grounding validator for a generic knowledge retrieval system.

            Your ONLY task is to determine whether every factual claim in the GENERATED ANSWER
            is supported by the KNOWLEDGE BASE.

            GROUNDING PRINCIPLE
            ========================================

            The GENERATED ANSWER may contain ONLY factual information that is explicitly
            supported by the KNOWLEDGE BASE.

            A claim is supported when the KNOWLEDGE BASE directly states the same fact
            or states information that is a faithful paraphrase of that fact.

            IMPORTANT:
            The knowledge base does NOT need to contain a formal definition, heading,
            explanation, or specific wording for a fact to be supported.

            Evaluate factual support, not whether the knowledge base contains a
            formal definition.

            STRICT RULES
            ========================================

            1. Identify each factual claim in the GENERATED ANSWER.

            2. Check each factual claim against the KNOWLEDGE BASE.

            3. Mark a claim as grounded when its factual meaning is explicitly supported
               by the KNOWLEDGE BASE.

            4. A faithful paraphrase of an explicitly stated fact is grounded.

            5. Do not require the exact wording used in the KNOWLEDGE BASE.

            6. Do not require a formal definition unless the GENERATED ANSWER itself
               makes a claim that requires information not present in the KNOWLEDGE BASE.

            7. Do not use pretrained knowledge.

            8. Do not use outside knowledge.

            9. Do not use assumptions.

            10. Do not use common knowledge.

            11. Do not use the USER QUESTION as evidence.

            12. Do not infer facts that are not stated in the KNOWLEDGE BASE.

            RELATIONSHIPS AND INFERENCES
            ========================================

            13. Do not infer a relationship merely because two concepts appear
                in the KNOWLEDGE BASE.

            14. If the KNOWLEDGE BASE states facts about two concepts separately,
                this does not automatically support a claim that those concepts
                are related.

            15. A relationship, comparison, dependency, interaction, compatibility,
                or connection is grounded only when that specific relationship is
                explicitly supported by the KNOWLEDGE BASE.

            16. Do not combine separate facts to create a new factual conclusion.

            PARTIAL ANSWERS
            ========================================

            17. The GENERATED ANSWER does not need to include every fact in the
                KNOWLEDGE BASE.

            18. Do not reject an answer because it omits information.

            19. Validate only the factual claims actually present in the GENERATED ANSWER.

            20. If every factual claim in the GENERATED ANSWER is supported,
                return grounded=true.

            21. If even one factual claim is unsupported, return grounded=false.

            22. When grounded=false, identify the specific unsupported claim.

            23. Do not introduce new facts when explaining why a claim is unsupported.

            NON-FACTUAL CONTENT
            ========================================

            24. Do not reject grammar, wording, formatting, or style.

            25. If the GENERATED ANSWER contains no factual claims, return grounded=true.

            OUTPUT FORMAT
            ========================================

            Return ONLY valid JSON using this exact structure:

            {
              "grounded": true,
              "reason": "brief reason"
            }

            For grounded=true:
            The reason must state that every factual claim in the GENERATED ANSWER
            is supported by the KNOWLEDGE BASE.

            For grounded=false:
            The reason must identify the specific unsupported factual claim.

            Do not return markdown.
            Do not return explanations outside the JSON.
            Do not return additional fields.
            """;

            string userPrompt = $@"========================================
    KNOWLEDGE BASE
    ========================================

    {knowledgeBase}

    ========================================
    USER QUESTION
    ========================================

    {question}

    ========================================
    GENERATED ANSWER
    ========================================

    {answer}";

            var schema = new
            {
                type = "object",
                properties = new
                {
                    grounded = new
                    {
                        type = "boolean"
                    },
                    reason = new
                    {
                        type = "string"
                    }
                },
                required = new[] { "grounded", "reason" },
                additionalProperties = false
            };

            Console.WriteLine(
                "========= KB SENT TO GROUNDING =========");

            Console.WriteLine(knowledgeBase);

            Console.WriteLine(
                "========= ANSWER SENT TO GROUNDING =========");

            Console.WriteLine(answer);

            var request = new OllamaChatRequest
            {
                Model = "llama3.2",
                Stream = false,
                Format = schema,

                Options = new OllamaGenerationOptions
                {
                    Temperature = 0f,
                    Seed = 42
                },

                Messages =
                [
                    new OllamaChatMessage
            {
                Role = "system",
                Content = systemPrompt
            },
            new OllamaChatMessage
            {
                Role = "user",
                Content = userPrompt
            }
                ]
            };

            var response =
                await _ollamaClient.SendAsync(request);

            var content =
                response.Message.Content.Trim();

            Console.WriteLine(
                "========= GROUNDING VALIDATOR RESPONSE =========");

            Console.WriteLine(content);

            Console.WriteLine(
                "=================================================");

            try
            {
                var result =
                    JsonSerializer.Deserialize<GroundingValidationResult>(
                        content,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (result is not null)
                {
                    return result;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine(
                    $"Grounding validator JSON error: {ex.Message}");
            }

            return new GroundingValidationResult
            {
                Grounded = false,
                Reason =
                    "The grounding validator returned an invalid response."
            };
        }

        private static bool IsDirectlySupported(
        string answer,
        IReadOnlyList<string> selectedEvidence)
        {
            if (string.IsNullOrWhiteSpace(answer) ||
                selectedEvidence.Count == 0)
            {
                return false;
            }

            var normalizedAnswer =
                NormalizeClaim(answer);

            if (string.IsNullOrWhiteSpace(normalizedAnswer))
            {
                return false;
            }

            foreach (var content in selectedEvidence)
            {
                if (string.IsNullOrWhiteSpace(content))
                {
                    continue;
                }

                var normalizedContent =
                    NormalizeClaim(content);

                // Exact normalized match.
                if (normalizedContent.Contains(
                    normalizedAnswer,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Compare the answer against individual KB sentences.
                var sentences =
                    content
                        .Split(
                            ['.', '!', '?'],
                            StringSplitOptions.RemoveEmptyEntries);

                foreach (var sentence in sentences)
                {
                    var normalizedSentence =
                        NormalizeClaim(sentence);

                    if (string.IsNullOrWhiteSpace(normalizedSentence))
                    {
                        continue;
                    }

                    if (ClaimsHaveSameMeaningStructure(
                        normalizedAnswer,
                        normalizedSentence))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ClaimsHaveSameMeaningStructure(
            string answer,
            string source)
        {
            var answerTokens =
                GetClaimTokens(answer);

            var sourceTokens =
                GetClaimTokens(source);

            if (answerTokens.Count == 0 ||
                sourceTokens.Count == 0)
            {
                return false;
            }

            var commonTokens =
                answerTokens
                    .Intersect(
                        sourceTokens,
                        StringComparer.OrdinalIgnoreCase)
                    .Count();

            var smallerCount =
                Math.Min(
                    answerTokens.Count,
                    sourceTokens.Count);

            if (smallerCount == 0)
            {
                return false;
            }

            var overlap =
                (float)commonTokens / smallerCount;

            // Require strong overlap and reasonably similar claim size.
            var sizeDifference =
                Math.Abs(
                    answerTokens.Count -
                    sourceTokens.Count);

            return overlap >= 0.90f &&
                   sizeDifference <= 2;
        }

        private static string NormalizeClaim(string text)
        {
            return string.Join(
                " ",
                text
                    .ToLowerInvariant()
                    .Split(
                        (char[]?)null,
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(token =>
                        token.Trim(
                            '.', ',', '!', '?', ':', ';',
                            '"', '\'', '(', ')', '[', ']'))
                    .Where(token =>
                        !string.IsNullOrWhiteSpace(token)));
        }

        private static List<string> GetClaimTokens(string text)
        {
            var ignoredWords =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)
                {
            "is",
            "are",
            "was",
            "were",
            "be",
            "being",
            "been",
            "stands",
            "for",
            "the",
            "a",
            "an"
                };

            return text
                .Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(token =>
                    token.Trim(
                        '.', ',', '!', '?', ':', ';',
                        '"', '\'', '(', ')', '[', ']'))
                .Where(token =>
                    token.Length >= 2 &&
                    !ignoredWords.Contains(token))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static List<string> GetMeaningfulTokens(string text)
        {
            return text
                .Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(token =>
                    token.Trim(
                        '.', ',', '!', '?', ':', ';',
                        '"', '\'', '(', ')', '[', ']'))
                .Where(token =>
                    token.Length >= 2)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string NormalizeText(string text)
        {
            return string.Join(
                " ",
                text
                    .Split(
                        (char[]?)null,
                        StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
