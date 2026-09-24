using AIChatAssistant.Configuration;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Ollama;
using AIChatAssistant.Models.RAG;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIChatAssistant.Services.AI.RAGPipeline
{
    public class EvidenceCompletenessValidator : IEvidenceCompletenessValidator
    {
        private readonly IOllamaClient _ollamaClient;
        private readonly OllamaOptions _options;
        public EvidenceCompletenessValidator(
        IOllamaClient ollamaClient,
        IOptions<OllamaOptions> options)
        {
            _ollamaClient = ollamaClient;
            _options = options.Value;
        }

        public async Task<EvidenceCompletenessResult> ValidateAsync(
            string informationNeed,
            IReadOnlyList<EvidenceUnit> selectedEvidence,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(informationNeed))
            {
                return new EvidenceCompletenessResult
                {
                    Complete = false,
                    Reason = "Information need was empty or invalid."
                };
            }

            if (selectedEvidence is null ||
                selectedEvidence.Count == 0)
            {
                return new EvidenceCompletenessResult
                {
                    Complete = false,
                    Reason = "No evidence was selected."
                };
            }

            var prompt =
                BuildPrompt(
                    informationNeed,
                    selectedEvidence);

            var request = new OllamaChatRequest
            {
                Model = _options.ChatModel,
                Stream = false,

                Messages =
                [
                    new OllamaChatMessage
                {
                    Role = "system",
                    Content = prompt
                }
                ],

                Format = new
                {
                    type = "object",
                    properties = new
                    {
                        complete = new
                        {
                            type = "boolean"
                        },
                        reason = new
                        {
                            type = "string"
                        }
                    },
                    required = new[]
                    {
                    "complete",
                    "reason"
                },
                    additionalProperties = false
                },

                Options = new OllamaGenerationOptions
                {
                    Temperature = 0,
                    Seed = 42
                }
            };

            var response =
                await _ollamaClient.SendAsync(request);

            Console.WriteLine(
                "========= EVIDENCE COMPLETENESS RAW RESPONSE =========");

            Console.WriteLine(
                response.Message.Content);

            Console.WriteLine(
                "=======================================================");

            var result =
                JsonSerializer.Deserialize<
                    EvidenceCompletenessResponse>(
                        response.Message.Content);

            if (result is null)
            {
                return new EvidenceCompletenessResult
                {
                    Complete = false,
                    Reason = "Validator returned an invalid response."
                };
            }

            return new EvidenceCompletenessResult
            {
                Complete = result.Complete,
                Reason = result.Reason ?? string.Empty
            };
        }

        private static string BuildPrompt(
    string informationNeed,
    IReadOnlyList<EvidenceUnit> selectedEvidence)
        {
            var prompt = """
            You are an evidence completeness validator.

            Your ONLY task is to decide whether the SELECTED EVIDENCE is sufficient
            to satisfy the EXACT INFORMATION NEED.

            IMPORTANT
            ========================================

            The INFORMATION NEED defines exactly what the user is asking for.

            Do not add anything to the information need.

            Do not infer additional requirements.

            Do not use outside knowledge.

            Do not answer the information need.

            Evaluate only the evidence provided below.

            RULES
            ========================================

            1. Read the INFORMATION NEED exactly as written.

            2. Determine what information the user explicitly requests.

            3. Check whether the SELECTED EVIDENCE directly supports everything
               explicitly requested.

            4. If the information need asks for one fact or definition, evidence
               supporting that fact or definition is sufficient.

            5. If the information need explicitly asks for multiple things, all
               requested things must be supported.

            6. Do not require relationships, comparisons, explanations, properties,
               or additional details unless they are explicitly requested.

            7. Do not infer missing information.

            8. Do not use knowledge that is not present in the SELECTED EVIDENCE.

            9. If the evidence is sufficient, return complete=true.

            10. If the evidence is insufficient, return complete=false and briefly
                state what explicitly requested information is missing.

            11. The reason must describe the actual INFORMATION NEED and evidence.
                Never return placeholder text such as "brief reason".

            12. Return ONLY valid JSON.

            INFORMATION NEED
            ========================================

            """;

                        prompt += informationNeed;

                        prompt += """

            SELECTED EVIDENCE
            ========================================

            """;

                        foreach (var evidence in selectedEvidence)
                        {
                            prompt +=
                                $"[Result {evidence.ResultIndex}] " +
                                $"[Evidence {evidence.EvidenceIndex}] " +
                                $"{evidence.Text}\n";
                        }

                        prompt += """

            DECISION
            ========================================

            If the evidence supports everything explicitly requested:

            {
              "complete": true,
              "reason": "The evidence directly supports the requested information."
            }

            If the evidence does not support everything explicitly requested:

            {
              "complete": false,
              "reason": "Briefly identify the specific requested information that is missing."
            }

            IMPORTANT:
            The examples above describe the JSON structure only.
            You must make your own decision from the INFORMATION NEED and
            SELECTED EVIDENCE provided above.

            Return ONLY the JSON object.
            """;

            return prompt;
        }
    }
    public class EvidenceCompletenessResponse
    {
        [JsonPropertyName("complete")]
        public bool Complete { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}
