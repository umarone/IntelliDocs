using AIChatAssistant.Models.Chat;

namespace AIChatAssistant.Services
{
    public interface IConversationService
    {
        List<ChatMessage> GetMessages(Guid conversationId);
        List<ChatMessage> GetRecentMessages(
        Guid conversationId,
        int maxMessages);
        void AddMessage(Guid conversationId, ChatMessage message);

        void ClearConversation(Guid conversationId);
    }
}
