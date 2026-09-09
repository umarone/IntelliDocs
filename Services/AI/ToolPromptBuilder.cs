using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Services.AI.Prompt;
using System.Runtime.Intrinsics.X86;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace AIChatAssistant.Services.AI
{
    public class ToolPromptSection : IPromptSection
    {
        private readonly IEnumerable<ITool> _tools;

        public ToolPromptSection(IEnumerable<ITool> tools)
        {
            _tools = tools;
        }

        public int Order => 20;

        public IEnumerable<OllamaChatMessage> Build(
            ChatRequest request,
            PromptContext context)
        {
            // Tool routing is now handled by IToolRouter.
            // The normal LLM request should not contain
            // tool instructions.
            yield break;
        }
    }
}
