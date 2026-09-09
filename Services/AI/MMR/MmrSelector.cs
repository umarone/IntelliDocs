using AIChatAssistant.Common;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.AI.Search;
using System.Xml.Linq;

namespace AIChatAssistant.Services.AI.MMR
{
    public class MmrSelector : IMmrSelector
    {
        public IReadOnlyList<SearchResult> Select(
            IReadOnlyList<SearchResult> candidates,
            int topK,
            float lambda)
        {
            if (candidates.Count == 0)
            {
                return [];
            }

            if (topK <= 0)
            {
                return [];
            }

            lambda = Math.Clamp(lambda, 0f, 1f);

            var selected = new List<SearchResult>();

            var remaining =
                candidates
                    .Where(x =>
                        x.Record.Vector is not null &&
                        x.Record.Vector.Length > 0)
                    .ToList();

            if (remaining.Count == 0)
            {
                return [];
            }

            // Start with the most relevant result.
            var first =
                remaining
                    .OrderByDescending(x => x.RerankScore)
                    .First();

            selected.Add(first);
            remaining.Remove(first);

            while (
                selected.Count < topK &&
                remaining.Count > 0)
            {
                SearchResult? bestCandidate = null;
                float bestMmrScore = float.MinValue;

                foreach (var candidate in remaining)
                {
                    var relevance =
                        candidate.RerankScore;

                    var maxSimilarity =
                        selected.Max(selectedResult =>
                            VectorMath.CosineSimilarity(
                                candidate.Record.Vector,
                                selectedResult.Record.Vector));

                    var mmrScore =
                        (lambda * relevance) -
                        ((1f - lambda) * maxSimilarity);

                    if (mmrScore > bestMmrScore)
                    {
                        bestMmrScore = mmrScore;
                        bestCandidate = candidate;
                    }
                }

                if (bestCandidate is null)
                {
                    break;
                }

                Console.WriteLine("========= MMR SELECTION =========");

                Console.WriteLine(
                    $"Selected RecordId: {bestCandidate.Record.Id}");

                Console.WriteLine(
                    $"Selected ChunkIndex: {bestCandidate.Record.ChunkIndex}");

                Console.WriteLine(
                    $"RerankScore: {bestCandidate.RerankScore:F3}");

                Console.WriteLine(
                    $"MMR Score: {bestMmrScore:F3}");

                Console.WriteLine(
                    $"Selected Count: {selected.Count + 1}");

                Console.WriteLine(
                    "=================================");

                selected.Add(bestCandidate);
                remaining.Remove(bestCandidate);
            }

            // Reassign final ranks.
            for (var index = 0;
                 index < selected.Count;
                 index++)
            {
                selected[index].Rank = index + 1;
            }

            return selected;

        }
    }
}