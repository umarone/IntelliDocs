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

        public List<DocumentChunk> CreateChunks(
            Guid documentId,
            string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return [];
            }

            if (_options.ChunkSize <= 0)
            {
                throw new InvalidOperationException(
                    "ChunkSize must be greater than zero.");
            }

            if (_options.ChunkOverlap < 0 ||
                _options.ChunkOverlap >= _options.ChunkSize)
            {
                throw new InvalidOperationException(
                    "ChunkOverlap must be zero or greater " +
                    "and smaller than ChunkSize.");
            }

            var chunks = new List<DocumentChunk>();

            var position = 0;
            var chunkIndex = 0;

            var step =
                _options.ChunkSize -
                _options.ChunkOverlap;

            while (position < content.Length)
            {
                var remainingLength =
                    content.Length - position;

                var length = Math.Min(
                    _options.ChunkSize,
                    remainingLength);

                var end = position + length;

                if (end < content.Length)
                {
                    var boundary = content.LastIndexOf(
                        ' ',
                        end - 1,
                        length);

                    if (boundary > position)
                    {
                        end = boundary;
                    }
                }

                var chunkContent =
                    content[position..end].Trim();

                if (!string.IsNullOrWhiteSpace(chunkContent))
                {
                    chunks.Add(new DocumentChunk
                    {
                        Id = Guid.NewGuid(),
                        DocumentId = documentId,
                        ChunkIndex = chunkIndex++,
                        Content = chunkContent
                    });
                }

                var nextPosition = end - _options.ChunkOverlap;

                if (nextPosition <= position)
                {
                    nextPosition = end;
                }

                position = nextPosition;
            }

            return chunks;
        }
    }
}
