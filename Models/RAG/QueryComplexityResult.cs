using AIChatAssistant.Common;

namespace AIChatAssistant.Models.RAG
{
    public class QueryComplexityResult
    {
        public QueryComplexity Complexity { get; init; }

        public double Score { get; init; }

        public string Reason { get; init; } = string.Empty;
    }
}
