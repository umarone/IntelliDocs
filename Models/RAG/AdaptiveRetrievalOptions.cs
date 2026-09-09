namespace AIChatAssistant.Models.RAG
{
    public class AdaptiveRetrievalOptions
    {
        public int SimpleTopK { get; set; } = 3;

        public int NormalTopK { get; set; } = 5;

        public int ComplexTopK { get; set; } = 8;

        public float SimpleThreshold { get; set; } = 0.65f;

        public float NormalThreshold { get; set; } = 0.55f;

        public float ComplexThreshold { get; set; } = 0.45f;
    }
}
