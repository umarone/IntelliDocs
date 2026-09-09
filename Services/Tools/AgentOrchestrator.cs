using AIChatAssistant.Common;
using AIChatAssistant.Configuration;
using AIChatAssistant.Enums;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models;
using AIChatAssistant.Models.Agent;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Ollama;
using AIChatAssistant.Models.Tools;
using AIChatAssistant.Models.Validators;
using AIChatAssistant.Services.AI.Ollama;
using Azure.Core;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace AIChatAssistant.Services.Tools
{
    public class AgentOrchestrator : IAgentOrchestrator
    {
        private const string KnowledgeSearchToolName = "KnowledgeSearch";

        private const string NoKnowledgeMessage =
            "I couldn't find relevant information in the knowledge base.";

        private const string InsufficientEvidenceMessage =
            "I couldn't find enough information in the knowledge base to answer this question.";

        private const string UngroundedAnswerMessage =
            "I couldn't generate a sufficiently grounded answer from the available knowledge base.";

        private readonly IToolExecutor _executor;
        private readonly IOllamaPromptBuilder _promptBuilder;
        private readonly IOllamaClient _ollamaClient;
        private readonly IToolRouter _toolRouter;
        private readonly IEnumerable<ITool> _tools;
        private readonly RagOptions _ragOptions;
        private readonly IGroundingValidator _groundingValidator;
        private readonly IKnowledgeRetriever _knowledgeRetriever;
        public AgentOrchestrator(
            IToolExecutor executor,
            IOllamaPromptBuilder promptBuilder,
            IOllamaClient ollamaClient,
            IToolRouter toolRouter,
            IEnumerable<ITool> tools,
            IOptions<RagOptions> ragOptions,
            IGroundingValidator groundingValidator,
            IKnowledgeRetriever knowledgeRetriever)
        {
            _executor = executor;
            _promptBuilder = promptBuilder;
            _ollamaClient = ollamaClient;
            _toolRouter = toolRouter;
            _tools = tools;
            _ragOptions = ragOptions.Value;
            _groundingValidator = groundingValidator;
            _knowledgeRetriever = knowledgeRetriever;
        }

        public async Task<AgentResult> RunAsync(
    ChatRequest request,
    OllamaChatRequest initialRequest)
        {
            var searchResults = new List<SearchResult>();

            // ==========================================
            // STEP 1: Determine whether a tool is needed
            // ==========================================

            var routingDecision =
                await _toolRouter.DecideAsync(request);

            LogRoutingDecision(routingDecision);

            // ==========================================
            // STEP 2: No tool required
            // ==========================================

            if (!routingDecision.UseTool)
            {
                var response =
                    await _ollamaClient.SendAsync(
                        initialRequest);

                return CreateAgentResult(
                    response,
                    searchResults);
            }

            // ==========================================
            // STEP 3: Execute selected tool
            // ==========================================

            var toolResult =
                await ExecuteToolAsync(
                    routingDecision, request, request.Filter);

            var toolResults =
                new List<ToolResult>
                {
            toolResult
                };

            Console.WriteLine(
                $"Tool results count: {toolResults.Count}");

            // ==========================================
            // STEP 4: Collect search results
            // ==========================================

            CollectSearchResults(
                toolResult,
                searchResults);

            // ==========================================
            // STEP 5: Validate knowledge evidence
            // ==========================================

            if (routingDecision.ToolName.Equals(
                    KnowledgeSearchToolName,
                    StringComparison.OrdinalIgnoreCase))
            {
                var evidenceResult =
                    ValidateEvidence(
                        searchResults);

                if (evidenceResult is not null)
                {
                    return CreateAgentResult(
                        evidenceResult,
                        searchResults);
                }
            }

            // ==========================================
            // STEP 6: Generate final answer
            // ==========================================

            var finalRequest =
                _promptBuilder.CreateToolResultRequest(
                    request,
                    toolResults);

            var finalResponse =
                await _ollamaClient.SendAsync(
                    finalRequest);

            Console.WriteLine(
    "========= FINAL LLM RESPONSE =========");

            Console.WriteLine(
                finalResponse.Message.Content);

            Console.WriteLine(
                "======================================");

            // ==========================================
            // STEP 7: Validate grounding
            // ==========================================

            var groundingResult =
                await ValidateGroundingAsync(
                    request,
                    finalResponse,
                    searchResults);

            if (!groundingResult.Grounded)
            {
                finalResponse =
                    CreateResponse(
                        UngroundedAnswerMessage);
            }

            return CreateAgentResult(
                finalResponse,
                searchResults);
        }


        public async Task<AgentResult> RunDocumentRagAsync(
    ChatRequest request,
    OllamaChatRequest initialRequest)
        {
            var latestUserMessage =
                request.Messages.LastOrDefault(
                    m => string.Equals(
                        m.Role,
                        "user",
                        StringComparison.OrdinalIgnoreCase));

            if (latestUserMessage is null ||
                string.IsNullOrWhiteSpace(latestUserMessage.Content))
            {
                return CreateAgentResult(
                    CreateResponse(
                        "I couldn't process the request because no user question was provided."),
                    []);
            }

            Console.WriteLine(
                "========= DIRECT DOCUMENT RAG =========");

            Console.WriteLine(
                $"Question: {latestUserMessage.Content}");

            // ==========================================
            // STEP 1: DIRECT KNOWLEDGE RETRIEVAL
            // ==========================================

            var searchResults =
                await _knowledgeRetriever.RetrieveAsync(
                    latestUserMessage.Content,
                    latestUserMessage.Content,
                    request.Filter);

            Console.WriteLine(
                $"Retrieved results: {searchResults.Count}");

            // ==========================================
            // STEP 2: VALIDATE RETRIEVED EVIDENCE
            // ==========================================

            var evidenceResult =
                ValidateEvidence(searchResults);

            if (evidenceResult is not null)
            {
                return CreateAgentResult(
                    evidenceResult,
                    searchResults);
            }

            // ==========================================
            // STEP 3: CREATE TOOL-RESULT SHAPE
            // ==========================================

            var toolResult =
                new ToolResult
                {
                    ToolName = KnowledgeSearchToolName,
                    Success = true,

                    Data = string.Join(
                        Environment.NewLine,
                        searchResults.Select(result =>
                            $"Similarity: {result.Similarity:F2}" +
                            Environment.NewLine +
                            result.Record.Content)),

                    Metadata = new Dictionary<string, object>
                    {
                        ["SearchResults"] = searchResults
                    }
                };

            // ==========================================
            // STEP 4: BUILD FINAL ANSWER REQUEST
            // ==========================================

            var finalRequest =
                _promptBuilder.CreateToolResultRequest(
                    request,
                    [toolResult]);

            // ==========================================
            // STEP 5: GENERATE FINAL ANSWER
            // ==========================================

            var finalResponse =
                await _ollamaClient.SendAsync(
                    finalRequest);

            Console.WriteLine(
                "========= FINAL LLM RESPONSE =========");

            Console.WriteLine(
                finalResponse.Message.Content);

            Console.WriteLine(
                "======================================");

            // ==========================================
            // STEP 6: VALIDATE GROUNDING
            // ==========================================

            var groundingResult =
                await ValidateGroundingAsync(
                    request,
                    finalResponse,
                    searchResults);

            if (!groundingResult.Grounded)
            {
                finalResponse =
                    CreateResponse(
                        UngroundedAnswerMessage);
            }

            Console.WriteLine(
                "======================================");

            return CreateAgentResult(
                finalResponse,
                searchResults);
        }


        // ============================================================
        // EVIDENCE VALIDATION
        // ============================================================

        private OllamaChatResponse? ValidateEvidence(
            IReadOnlyList<SearchResult> searchResults)
        {
            if (searchResults.Count == 0)
            {
                Console.WriteLine(
                    "No knowledge search results found.");

                return CreateResponse(
                    NoKnowledgeMessage);
            }

            var bestRerankScore =
                searchResults.Max(
                    x => x.RerankScore);

            Console.WriteLine(
                $"Best RerankScore: {bestRerankScore:F3}");

            Console.WriteLine(
                $"RerankThreshold: {_ragOptions.RerankThreshold:F3}");

            if (bestRerankScore <
                _ragOptions.RerankThreshold)
            {
                return CreateResponse(
                    InsufficientEvidenceMessage);
            }

            return null;
        }

        // ============================================================
        // GROUNDING VALIDATION
        // ============================================================

        private async Task<GroundingValidationResult>
            ValidateGroundingAsync(
                ChatRequest request,
                OllamaChatResponse response,
                IReadOnlyList<SearchResult> searchResults)
        {
            var latestUserMessage =
                request.Messages.Last(m =>
                    string.Equals(
                        m.Role,
                        "user",
                        StringComparison.OrdinalIgnoreCase));
            var groundingStart = Stopwatch.GetTimestamp();

            var groundingResult =
                await _groundingValidator.ValidateAsync(
                    latestUserMessage.Content,
                    response.Message.Content,
                    searchResults);

            var groundingElapsed =
                Stopwatch.GetElapsedTime(groundingStart);

            Console.WriteLine(
                $"Grounding Validator Total Time: {groundingElapsed.TotalMilliseconds:F0} ms");

            Console.WriteLine(
                "========= GROUNDING VALIDATION =========");

            Console.WriteLine(
                $"Grounded = {groundingResult.Grounded}");

            Console.WriteLine(
                $"Reason = {groundingResult.Reason}");

            Console.WriteLine(
                "========================================");

            return groundingResult;
        }

        // ============================================================
        // SEARCH RESULT COLLECTION
        // ============================================================

        private static void CollectSearchResults(
            ToolResult toolResult,
            List<SearchResult> searchResults)
        {
            if (toolResult.Metadata is null)
            {
                return;
            }

            if (!toolResult.Metadata.TryGetValue(
                    "SearchResults",
                    out var metadata))
            {
                return;
            }

            if (metadata is not IReadOnlyList<SearchResult> results)
            {
                return;
            }

            searchResults.AddRange(results);
        }

        // ============================================================
        // TOOL EXECUTION
        // ============================================================

        private async Task<ToolResult> ExecuteToolAsync(
            ToolRoutingDecision routingDecision, ChatRequest request, SearchFilter? filter)
        {
            if (string.IsNullOrWhiteSpace(
                    routingDecision.ToolName))
            {
                throw new InvalidOperationException(
                    "Tool router selected a tool but did not " +
                    "provide a tool name.");
            }

            var selectedTool =
                _tools.FirstOrDefault(tool =>
                    string.Equals(
                        tool.Definition.Name,
                        routingDecision.ToolName,
                        StringComparison.OrdinalIgnoreCase));

            if (selectedTool is null)
            {
                throw new InvalidOperationException(
                    $"Tool router selected an unknown tool " +
                    $"'{routingDecision.ToolName}'.");
            }

            ValidateRequiredParameters(
                selectedTool,
                routingDecision);

            ValidateSupportedParameters(
                selectedTool,
                routingDecision);

            var arguments = routingDecision.Arguments.ToDictionary(
            x => x.Key,
            x => (object)x.Value);

            if (string.Equals(
                    selectedTool.Definition.Name,
                    KnowledgeSearchToolName,
                    StringComparison.OrdinalIgnoreCase))
            {
                arguments["filter"] = filter;
            }

            var toolRequest =
                new ToolCallRequest
                {
                    ToolName =
                        selectedTool.Definition.Name,

                    Arguments = arguments,

                    OriginalQuestion = request.Messages.LastOrDefault(m => m.Role == "user")?.Content ?? string.Empty
                };
            Console.WriteLine(
    $"Knowledge Tool Query Argument: " +
    $"{routingDecision.Arguments.GetValueOrDefault("query")}");

            Console.WriteLine(
                $"Executing tool: {toolRequest.ToolName}");

            var toolResult =
                await _executor.ExecuteAsync(
                    toolRequest);

            Console.WriteLine(
                $"Tool result: Success={toolResult.Success}, " +
                $"Data='{toolResult.Data}', " +
                $"Error='{toolResult.Error}'");

            if (!toolResult.Success)
            {
                Console.WriteLine(
                    $"Tool execution failed: {toolResult.Error}");
            }

            return toolResult;
        }

        // ============================================================
        // TOOL PARAMETER VALIDATION
        // ============================================================

        private static void ValidateRequiredParameters(
            ITool selectedTool,
            ToolRoutingDecision routingDecision)
        {
            foreach (var parameter in
                     selectedTool.Definition.Parameters)
            {
                if (!routingDecision.Arguments.TryGetValue(
                        parameter,
                        out var value) ||
                    string.IsNullOrWhiteSpace(value))
                {
                    throw new InvalidOperationException(
                        $"Tool '{selectedTool.Definition.Name}' " +
                        $"requires parameter '{parameter}'.");
                }
            }
        }

        private static void ValidateSupportedParameters(
            ITool selectedTool,
            ToolRoutingDecision routingDecision)
        {
            foreach (var argument in
                     routingDecision.Arguments.Keys)
            {
                var supported =
                    selectedTool.Definition.Parameters.Any(
                        parameter =>
                            string.Equals(
                                parameter,
                                argument,
                                StringComparison.OrdinalIgnoreCase));

                if (!supported)
                {
                    throw new InvalidOperationException(
                        $"Tool '{selectedTool.Definition.Name}' " +
                        $"does not support parameter '{argument}'.");
                }
            }
        }

        // ============================================================
        // LOGGING
        // ============================================================

        private static void LogRoutingDecision(
            ToolRoutingDecision routingDecision)
        {
            Console.WriteLine(
                "========= AGENT ROUTER DECISION =========");

            Console.WriteLine(
                $"UseTool = {routingDecision.UseTool}");

            Console.WriteLine(
                $"ToolName = {routingDecision.ToolName}");

            Console.WriteLine(
                $"Arguments Count = " +
                $"{routingDecision.Arguments.Count}");

            Console.WriteLine(
                "==========================================");
        }

        // ============================================================
        // RESPONSE / RESULT HELPERS
        // ============================================================

        private static OllamaChatResponse CreateResponse(
            string content)
        {
            return new OllamaChatResponse
            {
                Message = new OllamaChatMessage
                {
                    Role = "assistant",
                    Content = content
                }
            };
        }

        private static AgentResult CreateAgentResult(
            OllamaChatResponse response,
            IReadOnlyList<SearchResult> searchResults)
        {
            return new AgentResult
            {
                Response = response,
                SearchResults = searchResults
            };
        }
    }
}
