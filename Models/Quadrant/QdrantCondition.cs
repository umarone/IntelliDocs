using System.Text.Json.Serialization;

namespace AIChatAssistant.Models.Quadrant
{
    public class QdrantCondition
    {
        public string Key { get; set; } = string.Empty;

        public QdrantMatch? Match { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public QdrantRange? Range { get; set; }
    }
}
