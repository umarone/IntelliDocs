namespace AIChatAssistant.Common
{
    public static class VectorMath
    {
        public static float CosineSimilarity(
float[] vector1,
float[] vector2)
        {

            if (vector1.Length != vector2.Length)
            {
                throw new InvalidOperationException(
                    "Vectors must have the same dimensions.");
            }
            float dotProduct = 0;
            float magnitude1 = 0;
            float magnitude2 = 0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];

                magnitude1 += vector1[i] * vector1[i];

                magnitude2 += vector2[i] * vector2[i];
            }

            magnitude1 = MathF.Sqrt(magnitude1);
            magnitude2 = MathF.Sqrt(magnitude2);

            return dotProduct / (magnitude1 * magnitude2);
        }
    }
}
