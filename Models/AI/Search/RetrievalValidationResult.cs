namespace AIChatAssistant.Models.AI.Search
{
    public class RetrievalValidationResult
    {
        public bool IsRelevant { get; set; }

        public float Confidence { get; set; }

        public float RetrievalConfidence { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}
