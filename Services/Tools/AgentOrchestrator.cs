using AIChatAssistant.Common;
using AIChatAssistant.Configuration;
using AIChatAssistant.Enums;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models;
using AIChatAssistant.Models.Agent;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Ollama;
using AIChatAssistant.Models.RAG;
using AIChatAssistant.Models.Tools;
using AIChatAssistant.Models.Validators;
using AIChatAssistant.Services.AI.Knowledge;
using AIChatAssistant.Services.AI.Ollama;
using Azure;
using Azure.Core;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

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
        private readonly IEvidenceCompletenessValidator _evidenceCompletenessValidator;
        private readonly IQueryDecomposer _queryDecomposer;
        public AgentOrchestrator(
            IToolExecutor executor,
            IOllamaPromptBuilder promptBuilder,
            IOllamaClient ollamaClient,
            IToolRouter toolRouter,
            IEnumerable<ITool> tools,
            IOptions<RagOptions> ragOptions,
            IGroundingValidator groundingValidator,
            IKnowledgeRetriever knowledgeRetriever,
            IEvidenceCompletenessValidator evidenceCompletenessValidator,
            IQueryDecomposer queryDecomposer)
        {
            _executor = executor;
            _promptBuilder = promptBuilder;
            _ollamaClient = ollamaClient;
            _toolRouter = toolRouter;
            _tools = tools;
            _ragOptions = ragOptions.Value;
            _groundingValidator = groundingValidator;
            _knowledgeRetriever = knowledgeRetriever;
            _evidenceCompletenessValidator = evidenceCompletenessValidator;
            _queryDecomposer = queryDecomposer;
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
                    toolResults,
                    []);

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
                    []);

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
                        CreateResponse("I couldn't process the request because no user question was provided."),[]);
                }

                Console.WriteLine(
                    "========= DIRECT DOCUMENT RAG =========");

                Console.WriteLine(
                    $"Question: {latestUserMessage.Content}");

                // ==========================================
                // STEP 1: DIRECT KNOWLEDGE RETRIEVAL
                // ==========================================

                var decomposition = await _queryDecomposer.DecomposeIfNeededAsync(latestUserMessage.Content);

                var informations =
                    decomposition.InformationNeeds;

                var complexityAnalysis =
                    decomposition.ComplexityAnalysis;
                Console.WriteLine(
                    "========= INFORMATION NEEDS =========");

                foreach (var informationNeed in informations)
                {
                    Console.WriteLine(
                        $"Information Need: {informationNeed}");
                }

                Console.WriteLine(
                    "=====================================");

                var retrievalResult =
                    await _knowledgeRetriever.RetrieveAsync(
                        latestUserMessage.Content,
                        latestUserMessage.Content,
                        request.Filter,
                        informations,
                        complexityAnalysis);

                var searchResults = retrievalResult.Results;

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
                // STEP 3: BUILD EVIDENCE UNITS
                // ==========================================

                var evidenceUnits =
                    BuildEvidenceUnits(searchResults);

            // ==========================================
            // STEP 4: INITIAL EVIDENCE SELECTION
            // ==========================================
            // Later to unblock
            //var evidenceTasks =
            //    informations.Select(
            //        async informationNeed =>
            //        {

            //            Console.WriteLine($"========= CURRENT INFORMATION NEED: [{informationNeed}] =========");
            //            var evidenceRequest =
            //                _promptBuilder.CreateEvidenceSelectionRequest(
            //                    request,
            //                    evidenceUnits,
            //                    informationNeed);

            //            var evidenceResponse =
            //                await _ollamaClient.SendAsync(
            //                    evidenceRequest);
            //            // TEMPORARY DEBUG
            //            Console.WriteLine(
            //                "========= RAW EVIDENCE SELECTION RESPONSE =========");

            //            Console.WriteLine(evidenceResponse.Message.Content);

            //            Console.WriteLine(
            //                "====================================================");

            //            return JsonSerializer.Deserialize<EvidenceSelectionResult>(
            //                evidenceResponse.Message.Content);
            //        });

            //var evidenceSelections =
            //    await Task.WhenAll(evidenceTasks);

            //
            var evidenceSelections = new List<EvidenceSelectionResult?>();

            foreach (var informationNeed in informations)
            {
                Console.WriteLine(
                    $"========= CURRENT INFORMATION NEED: [{informationNeed}] =========");

                var evidenceRequest =
                    _promptBuilder.CreateEvidenceSelectionRequest(
                        request,
                        evidenceUnits,
                        informationNeed);

                Console.WriteLine("========= FULL EVIDENCE SELECTION REQUEST =========");

                Console.WriteLine(
                    JsonSerializer.Serialize(
                        evidenceRequest,
                        new JsonSerializerOptions
                        {
                            WriteIndented = true
                        }));

                Console.WriteLine(
                    "===================================================");


                var evidenceResponse =
                    await _ollamaClient.SendAsync(
                        evidenceRequest);

                Console.WriteLine(
                    "========= RAW EVIDENCE SELECTION RESPONSE =========");

                Console.WriteLine(evidenceResponse.Message.Content);

                Console.WriteLine(
                    "====================================================");

                var selection =
                    JsonSerializer.Deserialize<EvidenceSelectionResult>(
                        evidenceResponse.Message.Content);

                evidenceSelections.Add(selection);
            }
            var combinedEvidenceSelection =
                    new EvidenceSelectionResult
                    {
                        Answers = []
                    };

                foreach (var evidenceSelection in evidenceSelections)
                {
                    if (evidenceSelection?.Answers is not null)
                    {
                        combinedEvidenceSelection.Answers.AddRange(
                            evidenceSelection.Answers);
                    }
                }

                var hasCompleteCoverage =
                    HasCompleteEvidenceCoverage(
                        combinedEvidenceSelection,
                        informations,
                        evidenceUnits);

                Console.WriteLine(
                    $"Evidence coverage complete: {hasCompleteCoverage}");

                if (!hasCompleteCoverage)
                {
                    return CreateAgentResult(
                        CreateResponse(
                            "I couldn't find sufficient evidence to answer all parts of your question."),
                        searchResults);
                }

                // ==========================================
                // STEP 4A: INITIAL EVIDENCE COMPLETENESS
                // ==========================================

                Console.WriteLine(
                    "========= EVIDENCE COMPLETENESS VALIDATION =========");

                var allEvidenceComplete = true;

                foreach (var informationNeed in informations)
                {
                    var evidenceAnswer =
                        combinedEvidenceSelection.Answers
                            .FirstOrDefault(
                                x => string.Equals(
                                    x.InformationNeed.Trim(),
                                    informationNeed.Trim(),
                                    StringComparison.OrdinalIgnoreCase));

                    if (evidenceAnswer is null)
                    {
                        Console.WriteLine(
                            $"Information Need: {informationNeed}");

                        Console.WriteLine(
                            "Validated Complete: False");

                        Console.WriteLine(
                            "Reason: No evidence-selection answer found.");

                        allEvidenceComplete = false;

                        continue;
                    }

                    var selectedEvidenceUnits =
                        new List<EvidenceUnit>();

                    foreach (var reference in evidenceAnswer.Evidence)
                    {
                        foreach (var evidenceIndex in reference.EvidenceIndexes)
                        {
                            var evidence =
                                evidenceUnits.FirstOrDefault(
                                    e =>
                                        e.ResultIndex ==
                                            reference.ResultIndex &&
                                        e.EvidenceIndex ==
                                            evidenceIndex);

                            if (evidence is not null &&
                                !string.IsNullOrWhiteSpace(evidence.Text))
                            {
                                selectedEvidenceUnits.Add(evidence);
                            }
                        }
                    }

                    var completenessResult =
                        await _evidenceCompletenessValidator.ValidateAsync(
                            informationNeed,
                            selectedEvidenceUnits);

                    Console.WriteLine(
                        $"Information Need: {informationNeed}");

                    Console.WriteLine(
                        $"LLM Complete Flag: {evidenceAnswer.Complete}");

                    Console.WriteLine(
                        $"Validated Complete: {completenessResult.Complete}");

                    Console.WriteLine(
                        $"Reason: {completenessResult.Reason}");

                    if (!completenessResult.Complete)
                    {
                        allEvidenceComplete = false;
                    }
                }

                Console.WriteLine(
                    $"All Evidence Complete: {allEvidenceComplete}");

                Console.WriteLine(
                    "===================================================");

                // ==========================================
                // STEP 4B: CORRECTIVE EVIDENCE SELECTION
                // ==========================================

                if (!allEvidenceComplete)
                {
                    Console.WriteLine(
                        "========= CORRECTIVE EVIDENCE SELECTION =========");

                    var correctiveEvidenceSelections =
                        new List<EvidenceSelectionResult>();

                    foreach (var informationNeed in informations)
                    {
                        var evidenceAnswer =
                            combinedEvidenceSelection.Answers
                                .FirstOrDefault(
                                    x => string.Equals(
                                        x.InformationNeed.Trim(),
                                        informationNeed.Trim(),
                                        StringComparison.OrdinalIgnoreCase));

                        if (evidenceAnswer is null)
                        {
                            continue;
                        }

                        var previouslySelectedEvidence =
                            new List<EvidenceUnit>();

                        foreach (var reference in evidenceAnswer.Evidence)
                        {
                            foreach (var evidenceIndex in reference.EvidenceIndexes)
                            {
                                var evidence =
                                    evidenceUnits.FirstOrDefault(
                                        e =>
                                            e.ResultIndex ==
                                                reference.ResultIndex &&
                                            e.EvidenceIndex ==
                                                evidenceIndex);

                                if (evidence is not null)
                                {
                                    previouslySelectedEvidence.Add(
                                        evidence);
                                }
                            }
                        }

                        var correctiveRequest =
                            _promptBuilder.CreateCorrectiveEvidenceSelectionRequest(
                                request,
                                evidenceUnits,
                                informationNeed,
                                previouslySelectedEvidence);

                        var correctiveResponse =
                            await _ollamaClient.SendAsync(
                                correctiveRequest);

                        Console.WriteLine(
                            "========= CORRECTIVE EVIDENCE RAW RESPONSE =========");

                        Console.WriteLine(
                            correctiveResponse.Message.Content);

                        Console.WriteLine(
                            "=====================================================");

                        var correctiveSelection =
                            JsonSerializer.Deserialize<EvidenceSelectionResult>(
                                correctiveResponse.Message.Content);

                        if (correctiveSelection is not null)
                        {
                            correctiveEvidenceSelections.Add(
                                correctiveSelection);
                        }
                    }

                    Console.WriteLine(
                        "========= MERGING CORRECTIVE EVIDENCE =========");

                    foreach (var correctiveSelection in
                             correctiveEvidenceSelections)
                    {
                        foreach (var correctiveAnswer in
                                 correctiveSelection.Answers)
                        {
                            var originalAnswer =
                                combinedEvidenceSelection.Answers
                                    .FirstOrDefault(
                                        x => string.Equals(
                                            x.InformationNeed.Trim(),
                                            correctiveAnswer.InformationNeed.Trim(),
                                            StringComparison.OrdinalIgnoreCase));

                            if (originalAnswer is null)
                            {
                                continue;
                            }

                            foreach (var correctiveReference in
                                     correctiveAnswer.Evidence)
                            {
                                var existingReference =
                                    originalAnswer.Evidence
                                        .FirstOrDefault(
                                            x =>
                                                x.ResultIndex ==
                                                    correctiveReference.ResultIndex);

                                if (existingReference is null)
                                {
                                    originalAnswer.Evidence.Add(
                                        correctiveReference);

                                    continue;
                                }

                                foreach (var evidenceIndex in
                                         correctiveReference.EvidenceIndexes)
                                {
                                    if (!existingReference.EvidenceIndexes.Contains(
                                            evidenceIndex))
                                    {
                                        existingReference.EvidenceIndexes.Add(
                                            evidenceIndex);
                                    }
                                }
                            }
                        }
                    }

                    Console.WriteLine(
                        "Combined evidence after corrective selection:");

                    foreach (var answer in
                             combinedEvidenceSelection.Answers)
                    {
                        Console.WriteLine(
                            $"Information Need: {answer.InformationNeed}");

                        foreach (var reference in answer.Evidence)
                        {
                            Console.WriteLine(
                                $"Result Index: {reference.ResultIndex}");

                            foreach (var evidenceIndex in
                                     reference.EvidenceIndexes)
                            {
                                Console.WriteLine(
                                    $"Evidence Index: {evidenceIndex}");

                                var evidence =
                                    evidenceUnits.FirstOrDefault(
                                        e =>
                                            e.ResultIndex ==
                                                reference.ResultIndex &&
                                            e.EvidenceIndex ==
                                                evidenceIndex);

                                Console.WriteLine(
                                    $"Evidence Text: {evidence?.Text}");
                            }
                        }
                    }

                    Console.WriteLine(
                        "================================================");

                    // ==========================================
                    // STEP 4C: CORRECTIVE COMPLETENESS VALIDATION
                    // ==========================================

                    Console.WriteLine(
                        "========= CORRECTIVE COMPLETENESS VALIDATION =========");

                    var correctiveCompletenessReasons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);


                    var correctiveEvidenceComplete = true;

                    foreach (var informationNeed in informations)
                    {
                        var evidenceAnswer =
                            combinedEvidenceSelection.Answers
                                .FirstOrDefault(
                                    x => string.Equals(
                                        x.InformationNeed.Trim(),
                                        informationNeed.Trim(),
                                        StringComparison.OrdinalIgnoreCase));

                        if (evidenceAnswer is null)
                        {
                            correctiveEvidenceComplete = false;

                            Console.WriteLine(
                                $"Information Need: {informationNeed}");

                            Console.WriteLine(
                                "Corrective Validated Complete: False");

                            Console.WriteLine(
                                "Reason: No evidence-selection answer found.");

                            continue;
                        }

                        var selectedEvidenceUnits =
                            new List<EvidenceUnit>();

                        foreach (var reference in evidenceAnswer.Evidence)
                        {
                            foreach (var evidenceIndex in
                                     reference.EvidenceIndexes)
                            {
                                var evidence =
                                    evidenceUnits.FirstOrDefault(
                                        e =>
                                            e.ResultIndex ==
                                                reference.ResultIndex &&
                                            e.EvidenceIndex ==
                                                evidenceIndex);

                                if (evidence is not null &&
                                    !string.IsNullOrWhiteSpace(evidence.Text))
                                {
                                    selectedEvidenceUnits.Add(evidence);
                                }
                            }
                        }

                        var completenessResult =
                            await _evidenceCompletenessValidator.ValidateAsync(
                                informationNeed,
                                selectedEvidenceUnits);

                        Console.WriteLine(
                            $"Information Need: {informationNeed}");

                        Console.WriteLine(
                            $"Corrective Validated Complete: " +
                            $"{completenessResult.Complete}");

                        Console.WriteLine(
                            $"Reason: {completenessResult.Reason}");

                        correctiveCompletenessReasons[informationNeed] = completenessResult.Reason;

                        if (!completenessResult.Complete)
                        {
                            correctiveEvidenceComplete = false;
                        }
                    }

                    Console.WriteLine(
                        $"Corrective Evidence Complete: " +
                        $"{correctiveEvidenceComplete}");

                    Console.WriteLine(
                        "======================================================");


                    if (!correctiveEvidenceComplete)
                    {
                        Console.WriteLine(
                            "========= STARTING CORRECTIVE RETRIEVAL =========");

                        foreach (var informationNeed in informations)
                        {
                            var completenessReason = correctiveCompletenessReasons.TryGetValue(informationNeed, out var reason) ? reason : string.Empty;

                            Console.WriteLine($"Corrective Complexity: {complexityAnalysis?.Complexity}");

                            Console.WriteLine(
                                $"Corrective Complexity Score: {complexityAnalysis?.Score}");

                            var correctiveRetrieval = await RetrieveCorrectiveEvidenceAsync(
                                informationNeed,
                                completenessReason,
                                request,
                                complexityAnalysis);

                            var newCorrectiveResults =
                            correctiveRetrieval.Results
                                .Where(corrective =>
                                    !searchResults.Any(
                                        original =>
                                            original.Record.Id ==
                                            corrective.Record.Id))
                                .ToList();

                            Console.WriteLine(
                                $"Corrective new results: {newCorrectiveResults.Count}");

                            Console.WriteLine(
                                $"Information Need: {informationNeed}");

                            Console.WriteLine(
                                $"Corrective Results: " +
                                $"{correctiveRetrieval.Results.Count}");

                            foreach (var result in correctiveRetrieval.Results)
                            {
                                Console.WriteLine(
                                    $"Corrective Result: " +
                                    $"{result.Record.Content}");
                            }
                        }

                        Console.WriteLine(
                            "==================================================");

                        return CreateAgentResult(
                            CreateResponse(
                                "I couldn't find sufficient evidence to answer all parts of your question."),
                            searchResults);
                    }

                }

                var evidenceSelectionResult =
                    combinedEvidenceSelection;

                // ==========================================
                // STEP 5: MAP EVIDENCE REFERENCES TO EXACT TEXT
                // ==========================================

                var selectedEvidence =
                    new List<string>();

                foreach (var answer in evidenceSelectionResult.Answers)
                {
                    foreach (var reference in answer.Evidence)
                    {
                        if (reference.ResultIndex < 0 ||
                            reference.ResultIndex >= searchResults.Count)
                        {
                            continue;
                        }

                        foreach (var evidenceIndex in reference.EvidenceIndexes)
                        {
                            var evidence =
                                evidenceUnits.FirstOrDefault(
                                    e =>
                                        e.ResultIndex ==
                                            reference.ResultIndex &&
                                        e.EvidenceIndex ==
                                            evidenceIndex);

                            if (evidence is not null &&
                                !string.IsNullOrWhiteSpace(evidence.Text))
                            {
                                selectedEvidence.Add(
                                    evidence.Text);
                            }
                        }
                    }
                }

                selectedEvidence =
                    selectedEvidence
                        .Distinct(StringComparer.Ordinal)
                        .ToList();

                Console.WriteLine(
                    $"Evidence answers: {evidenceSelectionResult.Answers.Count}");

                foreach (var answer in evidenceSelectionResult.Answers)
                {
                    Console.WriteLine(
                        $"Information Need: {answer.InformationNeed}");

                    foreach (var reference in answer.Evidence)
                    {
                        Console.WriteLine(
                            $"Result Index: {reference.ResultIndex}");

                        foreach (var evidenceIndex in reference.EvidenceIndexes)
                        {
                            Console.WriteLine(
                                $"Selected Evidence Index: {evidenceIndex}");
                        }
                    }
                }

                Console.WriteLine(
                    "========= EVIDENCE SELECTION RESPONSE =========");

                Console.WriteLine(
                    "Combined evidence selection responses processed.");

                Console.WriteLine(
                    "================================================");

                Console.WriteLine(
                    "========= SELECTED EVIDENCE =========");

                foreach (var evidence in selectedEvidence)
                {
                    Console.WriteLine(
                        evidence);
                }

                Console.WriteLine(
                    "=====================================");

                // ==========================================
                // STEP 6: CREATE FINAL ANSWER FROM SELECTED EVIDENCE
                // ==========================================

                var finalResponse =
                    CreateResponse(
                        string.Join(
                            Environment.NewLine,
                            selectedEvidence));

                Console.WriteLine(
                    "========= DETERMINISTIC FINAL ANSWER =========");

                Console.WriteLine(
                    finalResponse.Message.Content);

                Console.WriteLine(
                    "==============================================");

                // ==========================================
                // STEP 7: VALIDATE GROUNDING
                // ==========================================

                var groundingResult =
                    await ValidateGroundingAsync(
                        request,
                        finalResponse,
                        selectedEvidence);

                if (!groundingResult.Grounded)
                {
                    finalResponse =
                        CreateResponse(
                            UngroundedAnswerMessage);
                }

                Console.WriteLine(
                    "========= GROUNDING VALIDATION =========");

                Console.WriteLine(
                    $"Grounded = {groundingResult.Grounded}");

                Console.WriteLine(
                    $"Reason = {groundingResult.Reason}");

                Console.WriteLine(
                    "=========================================");

                // ==========================================
                // STEP 8: RETURN FINAL ANSWER
                // ==========================================

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
            //Previous
            //var bestRerankScore =
            //    searchResults.Max(
            //        x => x.RerankScore);
            //new
            var bestSimilarity =
                searchResults.Max(
                    x => x.Similarity);

            Console.WriteLine(
                 $"Best Similarity: {bestSimilarity:F3}");

            Console.WriteLine(
                $"SimilarityThreshold: {_ragOptions.RerankThreshold:F3}");
            //Old
            //if (bestRerankScore <
            //    _ragOptions.RerankThreshold)
            //{
            //    return CreateResponse(
            //        InsufficientEvidenceMessage);
            //}

            //New code

            if (bestSimilarity <
               _ragOptions.SimilarityThreshold)
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
        IReadOnlyList<string> selectedEvidence)
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
                    selectedEvidence);

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
        private static List<EvidenceUnit> BuildEvidenceUnits(
    IReadOnlyList<SearchResult> searchResults)
        {
            var units = new List<EvidenceUnit>();

            for (int resultIndex = 0;
                 resultIndex < searchResults.Count;
                 resultIndex++)
            {
                var content =
                    searchResults[resultIndex].Record.Content;

                if (string.IsNullOrWhiteSpace(content))
                    continue;

                var sentences =
                    Regex.Split(
                        content,
                        @"(?<=[.!?])\s+")
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                for (int evidenceIndex = 0;
                     evidenceIndex < sentences.Count;
                     evidenceIndex++)
                {
                    units.Add(
                        new EvidenceUnit
                        {
                            ResultIndex = resultIndex,
                            EvidenceIndex = evidenceIndex,
                            Text = sentences[evidenceIndex].Trim()
                        });
                }
            }
            // TEMPORARY DEBUG
            Console.WriteLine("========= EVIDENCE UNITS =========");

            foreach (var unit in units)
            {
                Console.WriteLine(
                    $"ResultIndex={unit.ResultIndex}, " +
                    $"EvidenceIndex={unit.EvidenceIndex}, " +
                    $"Text={unit.Text}");
            }

            Console.WriteLine("==================================");

            return units;
        }
        private static bool HasCompleteEvidenceCoverage(
     EvidenceSelectionResult? selection,
     IReadOnlyList<string> informationNeeds,
     IReadOnlyList<EvidenceUnit> evidenceUnits)
        {
            if (selection?.Answers is null)
                return false;

            foreach (var informationNeed in informationNeeds)
            {
                var answer =
                    selection.Answers.FirstOrDefault(
                        a => string.Equals(
                            a.InformationNeed.Trim(),
                            informationNeed.Trim(),
                            StringComparison.OrdinalIgnoreCase));

                if (answer is null)
                    return false;

                if (answer.Evidence is null ||
                    answer.Evidence.Count == 0)
                {
                    return false;
                }

                foreach (var reference in answer.Evidence)
                {
                    if (reference.ResultIndex < 0)
                        return false;

                    if (reference.EvidenceIndexes is null ||
                        reference.EvidenceIndexes.Count == 0)
                    {
                        return false;
                    }

                    foreach (var evidenceIndex in reference.EvidenceIndexes)
                    {
                        var evidenceExists =
                            evidenceUnits.Any(
                                e =>
                                    e.ResultIndex == reference.ResultIndex &&
                                    e.EvidenceIndex == evidenceIndex &&
                                    !string.IsNullOrWhiteSpace(e.Text));

                        if (!evidenceExists)
                            return false;
                    }
                }
            }

            return true;
        }

        private async Task<RetrievalResult> RetrieveCorrectiveEvidenceAsync(
    string informationNeed,
    string completenessReason,
    ChatRequest request,
    QueryComplexityResult? complexityAnalysis)
        {
            if (string.IsNullOrWhiteSpace(informationNeed))
            {
                return new RetrievalResult
                {
                    Results = [],
                    InformationNeeds = []
                };
            }

            Console.WriteLine(
                "========= CORRECTIVE RETRIEVAL =========");

            Console.WriteLine(
                $"Information Need: {informationNeed}");

            Console.WriteLine(
                $"Completeness Gap: {completenessReason}");

            Console.WriteLine(
                $"Incoming Complexity: " +
                $"{complexityAnalysis?.Complexity}");

            Console.WriteLine(
                $"Incoming Complexity Score: " +
                $"{complexityAnalysis?.Score}");

            var correctiveQuery =
                string.IsNullOrWhiteSpace(completenessReason)
                    ? informationNeed
                    : $"{informationNeed}. {completenessReason}";

            Console.WriteLine(
                $"Corrective Query: {correctiveQuery}");

            if (complexityAnalysis == null)
            {
                Console.WriteLine(
                    "WARNING: Complexity analysis is NULL " +
                    "inside RetrieveCorrectiveEvidenceAsync.");

                Console.WriteLine(
                    "=========================================");

                throw new InvalidOperationException(
                    "Complexity analysis was lost before corrective retrieval.");
            }

            var result =
                await _knowledgeRetriever.RetrieveAsync(
                    correctiveQuery,
                    informationNeed,
                    request.Filter,
                    [informationNeed],
                    complexityAnalysis);

            Console.WriteLine(
                $"Corrective retrieved results: " +
                $"{result.Results.Count}");

            Console.WriteLine(
                "=========================================");

            return result;
        }
    }
}
