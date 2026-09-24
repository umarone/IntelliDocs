using System.Text.Json.Serialization;

namespace AIChatAssistant.Models.RAG
{
    public class EvidenceSelectionResult
    {
        [JsonPropertyName("answers")]
        public List<EvidenceAnswer> Answers { get; set; } = [];
    }
}
