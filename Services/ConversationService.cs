using AIChatAssistant.Models.Chat;

namespace AIChatAssistant.Services
{
    public class ConversationService : IConversationService
    {
        private readonly Dictionary<Guid, List<ChatMessage>> _conversations = new();
        public void AddMessage(Guid conversationId, ChatMessage message)
        {
            if (!_conversations.TryGetValue(conversationId, out var conversation))
            {
                conversation = new List<ChatMessage>();

                _conversations.Add(conversationId, conversation);
            }

            conversation.Add(message);
        }
        public List<ChatMessage> GetRecentMessages(
    Guid conversationId,
    int maxMessages)
        {
            if (maxMessages <= 0)
            {
                return [];
            }

            if (!_conversations.TryGetValue(
                    conversationId,
                    out var conversation))
            {
                return [];
            }

            return conversation
                .TakeLast(maxMessages)
                .ToList();
        }
        public void ClearConversation(Guid conversationId)
        {
            _conversations.Remove(conversationId);
        }

        public List<ChatMessage> GetMessages(Guid conversationId)
        {
            if (_conversations.TryGetValue(conversationId, out var conversation))
            {
                return conversation;
            };
            return [];
        }
    }
}
