namespace AIChatAssistant.Models.RAG
{
    public class EvidenceCompletenessResult
    {
        public bool Complete { get; init; }
        public string Reason { get; init; } = string.Empty;
    }
}
