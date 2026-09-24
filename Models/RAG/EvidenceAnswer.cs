using System.Text.Json.Serialization;

namespace AIChatAssistant.Models.RAG
{
    public class EvidenceAnswer
    {
        [JsonPropertyName("informationNeed")]
        public string InformationNeed { get; set; } = string.Empty;

        [JsonPropertyName("evidence")]
        public List<EvidenceReference> Evidence { get; set; } = [];

        [JsonPropertyName("complete")]
        public bool Complete { get; set; }
    }
}
