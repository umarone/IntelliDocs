using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Tools;

namespace AIChatAssistant.Services.AI.Prompt
{
    public class PromptContext
    {
        public IReadOnlyList<SearchResult> SearchResults { get; init; } = [];
        public IReadOnlyList<ChatMessage> ConversationMessages { get; init; } = [];

        //public bool EnableTools { get; init; }

        // public ToolResult? ToolResult { get; init; }

        // public string? ToolCallJson { get; init; }
    }
}
