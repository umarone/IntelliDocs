namespace AIChatAssistant.Models.Quadrant
{
    public class QdrantPoint
    {
        public Guid Id { get; set; }

        public float[] Vector { get; set; } = [];

        public QdrantPayload Payload { get; set; } = new();
    }
}
