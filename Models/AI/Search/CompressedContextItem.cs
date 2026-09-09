namespace AIChatAssistant.Models.AI.Search
{
    public class CompressedContextItem
    {
        public string RecordId { get; set; } = string.Empty;

        public bool Relevant { get; set; }

        public string Content { get; set; } = string.Empty;
    }
}
