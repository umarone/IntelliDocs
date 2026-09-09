using AIChatAssistant.Configuration;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Documents;
using Microsoft.Extensions.Options;

namespace AIChatAssistant.Services.Documents.Chunking
{
    public class CharacterChunkingService : IChunkingService
    {
        private readonly ChunkingOptions _options;
        public CharacterChunkingService(
            IOptions<ChunkingOptions> options)
        {
            _options = options.Value;
        }
      
        public List<DocumentChunk> CreateChunks(Guid documentId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return new List<DocumentChunk>();
            }

            if (_options.ChunkSize <= 0)
            {
                throw new InvalidOperationException(
                    "ChunkSize must be greater than zero.");
            }

            if (_options.ChunkOverlap >= _options.ChunkSize)
            {
                throw new InvalidOperationException(
                    "ChunkOverlap must be smaller than ChunkSize.");
            }

            var chunks = new List<DocumentChunk>();

            var position = 0;
            var chunkIndex = 0;
            var overlap = content.Length <= _options.ChunkSize ? 0 : _options.ChunkOverlap;

            var step = _options.ChunkSize - overlap;
            //var step = _options.ChunkSize - _options.ChunkOverlap;

            while (position < content.Length)
            {
                var length = Math.Min(
                    _options.ChunkSize,
                    content.Length - position);

                var chunkContent = content.Substring(position, length);

                chunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid(),
                    DocumentId = documentId,
                    ChunkIndex = chunkIndex++,
                    Content = chunkContent
                });

                position += step;
            }

            return chunks;
        }
    }
}
