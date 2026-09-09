using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.Validators;

namespace AIChatAssistant.Interfaces
{
    public interface IGroundingValidator
    {
        Task<GroundingValidationResult> ValidateAsync(
       string question,
       string answer,
       IReadOnlyList<SearchResult> searchResults);
    }
}
