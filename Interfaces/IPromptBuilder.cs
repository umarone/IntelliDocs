using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.AI.VectorStore;
using AIChatAssistant.Services.AI;

namespace AIChatAssistant.Interfaces
{
    public interface IPromptBuilder
    {
        PromptMessages BuildPrompt(
        List<SearchResult> chunks,
        string question);
    }
}
