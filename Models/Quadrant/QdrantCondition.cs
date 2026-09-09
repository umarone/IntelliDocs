namespace AIChatAssistant.Models.Quadrant
{
    public class QdrantCondition
    {
        public string Key { get; set; } = string.Empty;

        public QdrantMatch? Match { get; set; }

        public QdrantRange? Range { get; set; }
    }
}
