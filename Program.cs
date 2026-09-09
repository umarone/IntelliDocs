using AIChatAssistant.Configuration;
using AIChatAssistant.Data;
using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.AI.SourceBuilder;
using AIChatAssistant.Models.Chat;
using AIChatAssistant.Models.RAG;
using AIChatAssistant.Services;
using AIChatAssistant.Services.AI;
using AIChatAssistant.Services.AI.Conversations;
using AIChatAssistant.Services.AI.Factories;
using AIChatAssistant.Services.AI.Knowledge;
using AIChatAssistant.Services.AI.MMR;
using AIChatAssistant.Services.AI.Ollama;
using AIChatAssistant.Services.AI.Prompt;
using AIChatAssistant.Services.AI.RAGPipeline;
using AIChatAssistant.Services.AI.RagPromptBuilder;
using AIChatAssistant.Services.AI.Transformer;
using AIChatAssistant.Services.AI.Validators;
using AIChatAssistant.Services.Conversation;
using AIChatAssistant.Services.Documents;
using AIChatAssistant.Services.Documents.Chunking;
using AIChatAssistant.Services.Documents.Embeddings.Providers;
using AIChatAssistant.Services.Documents.Factories;
using AIChatAssistant.Services.Documents.Loaders;
using AIChatAssistant.Services.Documents.VectorStores;
using AIChatAssistant.Services.Providers;
using AIChatAssistant.Services.Providers.Chat;
using AIChatAssistant.Services.Tools;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<OllamaOptions>(
    builder.Configuration.GetSection("Ollama"));
builder.Services.Configure<AIOptions>(
    builder.Configuration.GetSection("AI"));
builder.Services.Configure<ChunkingOptions>(
    builder.Configuration.GetSection("Chunking"));
builder.Services.Configure<RagOptions>(
    builder.Configuration.GetSection("RagOptions"));
builder.Services.Configure<AdaptiveRetrievalOptions>(
    builder.Configuration.GetSection("AdaptiveRetrieval"));
builder.Services.Configure<MemoryOptions>(
    builder.Configuration.GetSection("MemoryOptions"));
builder.Services.Configure<QdrantOptions>(
    builder.Configuration.GetSection("Qdrant"));

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IChatProvider>(sp =>
    sp.GetRequiredService<OllamaChatProvider>());
builder.Services.AddScoped<OllamaChatProvider>();
//builder.Services.AddScoped<IChatProvider, OllamaChatProvider>();
builder.Services.AddScoped<IChatProviderFactory, ChatProviderFactory>();
builder.Services.AddSingleton<IConversationService, ConversationService>();

builder.Services.AddScoped<IRagService, RagService>();

builder.Services.AddScoped<IDocumentProcessingService, DocumentProcessingService>();

builder.Services.AddScoped<IDocumentLoader, PdfDocumentLoader>();

builder.Services.AddScoped<IDocumentLoader, WordDocumentLoader>();

builder.Services.AddScoped<IDocumentLoader, TextDocumentLoader>();

builder.Services.AddScoped<IDocumentLoaderFactory, DocumentLoaderFactory>();
builder.Services.AddScoped<IChunkingService, CharacterChunkingService>();
builder.Services.AddHttpClient<IEmbeddingProvider, OllamaEmbeddingProvider>();

//builder.Services.AddSingleton<IVectorStore, InMemoryVectorStore>();
builder.Services.AddScoped<IVectorStore, SqlServerVectorStore>();
builder.Services.AddScoped<IPromptBuilder, RagPromptBuilder >();
builder.Services.AddScoped<IDocumentSearchService, DocumentSearchService>();
builder.Services.AddHttpClient<IOllamaClient, OllamaClient>();
builder.Services.AddScoped<IOllamaPromptBuilder, OllamaPromptBuilder>();
builder.Services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
builder.Services.AddSingleton<IDocumentRepository,
    InMemoryDocumentRepository>();
builder.Services.AddScoped<
    IVectorRecordFactory,
    VectorRecordFactory>();
builder.Services.AddTransient<IToolExecutor, ToolExecutor>();

builder.Services.AddTransient<ITool, CalculatorTool>();
builder.Services.AddTransient<ITool, DateTool>();
builder.Services.AddTransient<ITool, KnowledgeSearchTool>();

builder.Services.AddTransient<ICalculatorService, CalculatorService>();


builder.Services.AddScoped<IKnowledgeRetriever, KnowledgeRetriever>();
builder.Services.AddScoped<IPromptSection, GeneralPromptSection>();
builder.Services.AddScoped<IPromptSection,KnowledgePromptSection>();
builder.Services.AddScoped<
    IPromptSection,
    AIChatAssistant.Services.AI.Prompt.ToolPromptSection>();
builder.Services.AddScoped<IPromptComposer,PromptComposer>();
builder.Services.AddScoped<ISourceReferenceBuilder, SourceReferenceBuilder>();
builder.Services.AddScoped<IConversationMemory, ConversationMemory>();
builder.Services.AddScoped<IPromptSection, ConversationMemorySection>();
builder.Services.AddSingleton<IPromptFormatter, PromptFormatter>();
builder.Services.AddSingleton<IConversationContextSelector,
    ConversationContextSelector>();
builder.Services.AddScoped<IToolRouter, ToolRouter>();
builder.Services.AddScoped<IKeywordSearchService, KeywordSearchService>();
builder.Services.AddScoped<IHybridRetriever, HybridRetriever>();
builder.Services.AddScoped<IReranker,Reranker>();
builder.Services.AddScoped<IGroundingValidator, GroundingValidator>();

builder.Services.AddScoped<IChatCompletionService, OllamaChatCompletionService>();
builder.Services.AddScoped<IQueryTransformer, QueryTransformer>();
builder.Services.AddScoped<IMultiQueryGenerator, MultiQueryGenerator>();
builder.Services.AddScoped<IQueryDecomposer, QueryDecomposer>();
builder.Services.AddScoped<IContextualCompressor, ContextualCompressor>();
builder.Services.AddScoped<IRetrievalValidator, RetrievalValidator>();
builder.Services.AddScoped<ICorrectiveQueryGenerator, CorrectiveQueryGenerator>();
builder.Services.AddSingleton<IQueryComplexityAnalyzer, RuleBasedQueryComplexityAnalyzer>();
builder.Services.AddScoped<AdaptiveRetrievalStrategy>();
builder.Services.AddSingleton<IMmrSelector, MmrSelector>();
builder.Services.AddSingleton<RetrievalConfidenceCalculator>();

///Quadrant Configurations
builder.Services.AddHttpClient<IVectorStore, QdrantVectorStore>();
var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
