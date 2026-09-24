namespace AIChatAssistant.Models.RAG
{
    public class EvidenceUnit
    {
        public int ResultIndex { get; init; }

        public int EvidenceIndex { get; init; }

        public string Text { get; init; } = string.Empty;
    }
}
