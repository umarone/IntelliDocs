namespace AIChatAssistant.Models.RAG
{
    public class InformationNeedDetectionResult
    {
        public bool HasMultipleNeeds { get; init; }
        public IReadOnlyList<string> InformationNeeds { get; init; } = [];
        public string Reason { get; init; } = string.Empty;
    }
}
