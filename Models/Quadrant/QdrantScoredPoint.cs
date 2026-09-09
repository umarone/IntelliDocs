using System.Text.Json.Serialization;

namespace AIChatAssistant.Models.Quadrant
{
    public class QdrantScoredPoint
    {
        public Guid Id { get; set; }
        public float Score { get; set; }
        public float[] Vector { get; set; } = [];
        public QdrantPayload Payload { get; set; } = new();
    }
}
