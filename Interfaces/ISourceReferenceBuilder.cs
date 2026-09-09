using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.Chat;

namespace AIChatAssistant.Interfaces
{
    public interface ISourceReferenceBuilder
    {
        List<SourceReference> Build(
        IReadOnlyList<SearchResult> results);
    }
}
