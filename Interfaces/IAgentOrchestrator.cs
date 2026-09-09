using AIChatAssistant.Models.Agent;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.Ollama;
using System.Threading.Tasks;

namespace AIChatAssistant.Interfaces
{
    public interface IAgentOrchestrator
    {
        Task<AgentResult> RunAsync(
        ChatRequest request,
        OllamaChatRequest initialRequest);

        Task<AgentResult> RunDocumentRagAsync(
        ChatRequest request,
        OllamaChatRequest initialRequest);
    }
}
    
