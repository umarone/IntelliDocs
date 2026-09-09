using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.Chat;

namespace AIChatAssistant.Models.AI.SourceBuilder
{
    public class SourceReferenceBuilder : ISourceReferenceBuilder
    {
        public List<SourceReference> Build(
            IReadOnlyList<SearchResult> results)
        {
            return results
                .OrderBy(r => r.Rank)
                .Select(r => new SourceReference
                {
                    Rank = r.Rank,
                    Similarity = MathF.Round(r.Similarity, 3),

                    DocumentId = r.Record.DocumentId,

                    ChunkIndex = r.Record.ChunkIndex,

                    FileName = r.Record.FileName,

                    ContentLength = r.Record.Content.Length
                })
                .ToList();
        }
    }
}
