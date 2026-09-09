using AIChatAssistant.Configuration;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models;
using AIChatAssistant.Models.AI.Search;
using AIChatAssistant.Models.AI.VectorStore;
using Microsoft.Extensions.Options;

namespace AIChatAssistant.Services.AI
{
    public class DocumentSearchService : IDocumentSearchService
    {
        private readonly RagOptions _options;
        private readonly IHybridRetriever _hybridRetriever;
        public DocumentSearchService(IOptions<RagOptions> options, IHybridRetriever hybridRetriever)
        {
            _options = options.Value;
            _hybridRetriever = hybridRetriever;
        }
        public async Task<List<SearchResult>> SearchAsync(string question, SearchFilter? filter, int topK)
        {
           var results = await _hybridRetriever.SearchAsync(
           question,
           question,
           filter,
           _options.TopK);

            return results;

        }
       
    }
}
