using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Chat;
using System.Text;

namespace AIChatAssistant.Services.AI.Prompt
{
    public class ConversationMemorySection : IPromptSection
    {
        public int Order => 15;
        private readonly IConversationMemory _conversationMemory;
        private readonly IPromptFormatter _promptFormatter;
        public ConversationMemorySection(IConversationMemory conversationMemory, IPromptFormatter promptFormatter)
        {
            _conversationMemory = conversationMemory;
            _promptFormatter = promptFormatter;
        }
        public IEnumerable<OllamaChatMessage> Build(ChatRequest request, PromptContext context)
        {
            var currentMessage = request.Messages.Last();

            var history = _conversationMemory.GetHistory(
                request.ConversationId,
                currentMessage);

            if (!history.Any())
            {
                yield break;
            }
            var lines = new List<string>();
          
            foreach (var message in history)
            {
                lines.Add($"{message.Role}:");
                lines.Add(message.Content);
                lines.Add(string.Empty);
            }

            yield return new OllamaChatMessage
            {
                Role = "system",
                Content =_promptFormatter.Build("Use the following conversation history to answer the user's question.", "Conversation History", lines)
            };
        }
    }
}
