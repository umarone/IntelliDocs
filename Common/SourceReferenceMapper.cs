using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.Chat;

namespace AIChatAssistant.Common
{
    public static class SourceReferenceMapper
    {
        public static List<SourceReference> Map(
        IReadOnlyList<SearchResult> results)
        {
            return results
                .Select(r => new SourceReference
                {
                    FileName = r.Record.FileName,
                    ChunkIndex = r.Record.ChunkIndex,
                    Similarity = r.Similarity
                })
                .ToList();
        }
    }
}
