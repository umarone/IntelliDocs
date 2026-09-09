using AIChatAssistant.Interfaces;
using AIChatAssistant.Models;
using AIChatAssistant.Models.Tools;

namespace AIChatAssistant.Services.Tools
{
    public class KnowledgeSearchTool : ITool
    {
        private readonly IKnowledgeRetriever _knowledgeRetriever;

        public KnowledgeSearchTool(
            IKnowledgeRetriever knowledgeRetriever)
        {
            _knowledgeRetriever = knowledgeRetriever;
        }
        public ToolDefinition Definition => new()
        {
            Name = "KnowledgeSearch",

            Description =
               "Searches the application's knowledge base and stored documents " +
                "for information needed to answer the user's question. " +
                "Use this tool when the user asks for factual, explanatory, " +
                "technical, company, project, or documentation-related information " +
                "that may be available in the knowledge base. " +
                "Prefer this tool over relying on general model knowledge when " +
                "the question can reasonably be answered from the application's " +
                "stored knowledge.",

            Parameters =
            [
                "query"
            ],

            WhenToUse =
            [
                "The answer may be contained in the application's knowledge base.",
                "The user asks about stored documents or documentation.",
                "The user asks for an explanation of a technical concept that may exist in the knowledge base.",
                "The user asks for factual information that should be grounded in application knowledge."
            ],

            Examples =
            [
                "What is OAuth?",
                "How does JWT work?",
                "Explain OpenID Connect."
            ]
        };

        public async Task<ToolResult> ExecuteAsync(ToolCallRequest request)
        {
            if (!request.Arguments.TryGetValue(
                    "query",
                    out var queryValue))
            {
                return new ToolResult
                {
                    ToolName = Definition.Name,
                    Success = false,
                    Error = "Query is required."
                };
            }

            var query = queryValue?.ToString();

            if (string.IsNullOrWhiteSpace(query))
            {
                return new ToolResult
                {
                    ToolName = Definition.Name,
                    Success = false,
                    Error = "Query cannot be empty."
                };
            }

            SearchFilter? filter = null;

            if (request.Arguments.TryGetValue(
                    "filter",
                    out var filterValue) &&
                filterValue is SearchFilter searchFilter)
            {
                filter = searchFilter;
            }
            var results =
                await _knowledgeRetriever.RetrieveAsync(query, request.OriginalQuestion , filter);

            if (results.Count == 0)
            {
                return new ToolResult
                {
                    ToolName = Definition.Name,
                    Success = true,
                    Data = "No relevant information was found."
                };
            }

            var lines = new List<string>();

            foreach (var result in results)
            {
                lines.Add(
                    $"Similarity: {result.Similarity:F2}");

                lines.Add(
                    result.Record.Content);

                lines.Add(string.Empty);
            }

            return new ToolResult
            {
                ToolName = Definition.Name,
                Success = true,
                Data = string.Join(
                    Environment.NewLine,
                    lines),
                Metadata = new Dictionary<string, object>
                {
                    ["SearchResults"] = results
                }
            };
        }
    }
}
