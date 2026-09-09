using AIChatAssistant.Models.RAG;

namespace AIChatAssistant.Interfaces
{
    public interface IQueryComplexityAnalyzer
    {
        Task<QueryComplexityResult> AnalyzeAsync(
       string question);
    }
}
