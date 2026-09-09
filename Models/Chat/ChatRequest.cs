 namespace AIChatAssistant.Models.Chat
{
    public class ChatRequest
    {
        //public string Prompt { get; set; } = string.Empty;
        public Guid ConversationId { get; set; }
        public List<ChatMessage> Messages { get; set; } = new();
        public SearchFilter? Filter { get; set; }
    }
}