using AIChatAssistant.Models;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.RAG;

namespace AIChatAssistant.Interfaces
{
    public interface IKnowledgeRetriever
    {
        Task<RetrievalResult> RetrieveAsync(
       string question,
       string originalQuestion,
       SearchFilter? filter = null,
       IReadOnlyList<string>? informationNeeds = null,
        QueryComplexityResult? complexityAnalysis = null);
    }
}
