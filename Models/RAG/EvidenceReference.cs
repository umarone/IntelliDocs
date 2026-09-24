using System.Text.Json.Serialization;

namespace AIChatAssistant.Models.RAG
{
    public class EvidenceReference
    {
        [JsonPropertyName("resultIndex")]
        public int ResultIndex { get; set; }

        [JsonPropertyName("evidenceIndexes")]
        public List<int> EvidenceIndexes { get; set; } = [];
    }
}
