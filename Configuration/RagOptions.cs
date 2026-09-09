namespace AIChatAssistant.Configuration
{
    public class RagOptions
    {
        public int TopK { get; set; } = 5;
        public float SimilarityThreshold { get; set; } = 0.5f;
        public float RerankThreshold { get; set; } = 0.5f;
        public int ContextWindow { get; set; }

        // Corrective retrieval
        public int MaxCorrectiveRetrievalAttempts { get; set; } = 1;

        public float MinimumRetrievalConfidence { get; set; } = 0.70f;
        public float RrfK { get; set; } = 60f;
        public float MmrLambda { get; set; } = 0.7f;

        public bool EnableMmr { get; set; } = true;
    }
}
