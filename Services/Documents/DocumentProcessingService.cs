using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.AI.Embeddings;
using AIChatAssistant.Models.AI.VectorStore;
using AIChatAssistant.Models.Documents;
using System.Reflection.Metadata;
using Document = AIChatAssistant.Models.Documents.Document;

namespace AIChatAssistant.Services.Documents
{
    public class DocumentProcessingService : IDocumentProcessingService
    {
        private readonly IDocumentLoaderFactory _loaderFactory;
        private readonly IChunkingService _chunkingService;
        private readonly IEmbeddingProvider _embeddingProvider;
        private readonly IVectorStore _vectorStore;
        private readonly IVectorRecordFactory _vectorRecordFactory;
        private readonly IDocumentRepository _documentMetadataService;
        public DocumentProcessingService(
    IDocumentLoaderFactory loaderFactory,
    IChunkingService chunkingService,
    IEmbeddingProvider embeddingProvider,
    IVectorStore vectorStore,
    IVectorRecordFactory vectorRecordFactory,
    IDocumentRepository documentMetadataService)
        {
            _loaderFactory = loaderFactory;
            _chunkingService = chunkingService;
            _embeddingProvider = embeddingProvider;
            _vectorStore = vectorStore;
            _vectorRecordFactory = vectorRecordFactory;
            _documentMetadataService = documentMetadataService;
        }
        public async Task ProcessAsync(Document document)
        {
            await _documentMetadataService.SaveAsync(document);

            var content = await LoadContentAsync(document);

            var chunks = CreateChunks(document, content);

            var embeddings = await GenerateEmbeddingsAsync(chunks);

            var vectorRecords = _vectorRecordFactory.Create(document, chunks, embeddings); //BuildVectorRecords(chunks, embeddings, document);

            await SaveVectorsAsync(vectorRecords);
        }
        private async Task<string> LoadContentAsync(Document document)
        {
            var extension = Path.GetExtension(document.FileName);

            var loader = _loaderFactory.GetLoader(extension);
            return await loader.LoadAsync(document.Content);
        }

        private List<DocumentChunk> CreateChunks(
            Document document,
            string content)
        {
            return _chunkingService.CreateChunks(document.DocumentId, content);
        }

        private Task<EmbeddingResponse> GenerateEmbeddingsAsync(
            List<DocumentChunk> chunks)
        {
            var request = new EmbeddingRequest() {
                Chunks = chunks
            };
            return _embeddingProvider.GenerateEmbeddingsAsync(request);
        }

        private Task SaveVectorsAsync(
            List<VectorRecord> records)
        {
            return _vectorStore.SaveAsync(records);    
            
        }
    }
}
