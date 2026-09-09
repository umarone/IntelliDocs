using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Ollama;
using AIChatAssistant.Models.Tools;

namespace AIChatAssistant.Interfaces
{
    public interface IOllamaPromptBuilder
    {
        OllamaChatRequest CreateChatRequest(ChatRequest request);
        OllamaChatRequest CreateChatRequest(
        ChatRequest request,
        IReadOnlyList<SearchResult> searchResults);
        OllamaChatRequest CreateToolResultRequest(
            ChatRequest request,
            IReadOnlyList<ToolResult> toolResults);
    }
}
