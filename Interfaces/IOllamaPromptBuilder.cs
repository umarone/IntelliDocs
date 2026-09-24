using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Ollama;
using AIChatAssistant.Models.RAG;
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
        IReadOnlyList<ToolResult> toolResults,
        IReadOnlyList<string> informationNeeds);

        public OllamaChatRequest CreateEvidenceSelectionRequest(
        ChatRequest request,
        IReadOnlyList<EvidenceUnit> evidenceUnits,
        string informationNeed);
        public OllamaChatRequest CreateCorrectiveEvidenceSelectionRequest(
        ChatRequest request,
        IReadOnlyList<EvidenceUnit> evidenceUnits,
        string informationNeed,
        IReadOnlyList<EvidenceUnit> previouslySelectedEvidence);
    }
}
