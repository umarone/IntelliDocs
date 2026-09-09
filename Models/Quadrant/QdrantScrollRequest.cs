namespace AIChatAssistant.Models.Quadrant
{
    public class QdrantScrollRequest
    {
        public QdrantFilter? Filter { get; set; }

        public int Limit { get; set; }

        public bool WithPayload { get; set; } = true;

        public bool WithVector { get; set; } = true;
    }
}
