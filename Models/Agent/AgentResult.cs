using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.Ollama;

namespace AIChatAssistant.Models.Agent
{
    public class AgentResult
    {
        public OllamaChatResponse Response { get; init; } = default!;

        public IReadOnlyList<SearchResult> SearchResults { get; init; }
            = [];
    }
}
