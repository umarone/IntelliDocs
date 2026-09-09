using AIChatAssistant.Configuration;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Chat;
using Microsoft.Extensions.Options;

namespace AIChatAssistant.Services.AI.Conversations
{
    public class ConversationMemory : IConversationMemory
    {
        private readonly IConversationService _conversationService;
        private readonly MemoryOptions _options;
        private readonly IConversationContextSelector _contextSelector;
        public ConversationMemory(
            IConversationService conversationService,
            IOptions<MemoryOptions> options,
            IConversationContextSelector contextSelector)
        {
            _conversationService = conversationService;
            _options = options.Value;
            _contextSelector = contextSelector;
        }

        public IReadOnlyList<ChatMessage> GetHistory(
        Guid conversationId, ChatMessage currentMessage)
        {
            var history = _conversationService.GetMessages(conversationId);

            if (history.Count == 0)
            {
                return [];
            }
            var previousMessages = history.Take(Math.Max(0, history.Count - 1))
                .ToList();

            return _contextSelector.SelectRelevantHistory(previousMessages, currentMessage);
        }
    }
}

