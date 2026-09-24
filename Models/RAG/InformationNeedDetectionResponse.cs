using System.Text.Json.Serialization;

namespace AIChatAssistant.Models.RAG
{
    public class InformationNeedDetectionResponse
    {
        [JsonPropertyName("informationNeeds")]
        public List<string> InformationNeeds { get; set; } = [];
    }
}
