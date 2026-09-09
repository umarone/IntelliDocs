namespace AIChatAssistant.Models.Chat
{
    public class ChatResponse
    {
        public ChatMessage Message { get; set; } = new();
        public List<SourceReference> Sources { get; set; } = new();
    }
}
