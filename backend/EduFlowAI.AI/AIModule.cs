using Amazon.S3;
using EduFlowAI.AI.Application.DocumentVerification;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Application.Services;
using EduFlowAI.AI.Infrastructure.Caching;
using EduFlowAI.AI.Infrastructure.DocumentVerification;
using EduFlowAI.AI.Infrastructure.DocumentVerification.Gemini;
using EduFlowAI.AI.Infrastructure.ExternalServices;
using EduFlowAI.AI.Infrastructure.ExternalServices.Gemini;
using EduFlowAI.AI.Infrastructure.Indexing;
using EduFlowAI.AI.Infrastructure.Options;
using EduFlowAI.AI.Infrastructure.Persistence;
using EduFlowAI.AI.Infrastructure.Processing;
using EduFlowAI.AI.Infrastructure.Services;
using EduFlowAI.AI.Infrastructure.Storage;
using EduFlowAI.AI.Presentation.Interfaces;
using EduFlowAI.AI.Presentation.Services;
using EduFlowAI.Documents.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace EduFlowAI.AI;

public static class AIModule
{
    public static IServiceCollection AddAIModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddKnowledgeAndIntentServices();
        services.AddRecommendationServices(configuration);

        return services;
    }

    // Knowledge base, RAG and intent routing.
    private static void AddKnowledgeAndIntentServices(this IServiceCollection services)
    {
        services.AddTransient<PdfProcessingService>();

        services.AddOptions<GeminiOptions>()
            .BindConfiguration(GeminiOptions.SectionName);

        services.AddOptions<DocumentStorageOptions>()
            .BindConfiguration(DocumentStorageOptions.SectionName);

        services.AddOptions<ChatSessionOptions>()
            .BindConfiguration(ChatSessionOptions.SectionName);

        services.AddOptions<IntentClassificationOptions>()
            .BindConfiguration(IntentClassificationOptions.SectionName);

        services.AddOptions<IngestionOptions>()
            .BindConfiguration(IngestionOptions.SectionName);

        services.AddOptions<RetrievalOptions>()
            .BindConfiguration(RetrievalOptions.SectionName);

        services.AddScoped<IDocumentTextExtractor, DocumentTextExtractor>();

        // Injected so session expiry can be tested with a fake clock.
        services.TryAddSingleton(TimeProvider.System);

        services.AddAWSService<IAmazonS3>();

        services.AddSingleton<IFileStorageService, S3FileStorageService>();

        services.AddHttpClient<IEmbeddingService, GeminiEmbeddingService>();
        services.AddHttpClient<IGeminiChatClient, GeminiChatClient>();

        // Concrete too: the background worker needs IngestDocumentAsync, not on the caller-facing interface.
        services.AddScoped<KnowledgeBaseService>();
        services.AddScoped<IKnowledgeIndexingService>(sp => sp.GetRequiredService<KnowledgeBaseService>());
        services.AddScoped<IKnowledgeRetrievalService, KnowledgeBaseRetrievalService>();
        services.AddScoped<IRagAnswerService, AiChatService>();
        services.AddScoped<IIntentRouter, IntentClassifierService>();

        // Phrases the applicant's document statuses warmly, grounded in the facts (§ document_status).
        services.AddScoped<IDocumentStatusNarrator, DocumentStatusNarrator>();

        // Narrates the application status, which is otherwise internal wording (§ application_status).
        services.AddScoped<IApplicationStatusNarrator, ApplicationStatusNarrator>();

        services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();

        services.AddSingleton<ISessionManager, InMemorySessionManager>();
        services.AddSingleton<KnowledgeBaseSyncState>();

        // Registered before the worker so a dimension mismatch fails startup before anything indexes.
        services.AddHostedService<EmbeddingDimensionGuard>();

        services.AddSingleton<IIndexingQueue, IndexingQueue>();
        services.AddHostedService<KnowledgeIndexingWorker>();

        services.AddSingleton<EmbeddingCacheService>();
    }

    // Track recommendation (Kamel).
    private static void AddRecommendationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<GeminiRecommendationOptions>()
            .Bind(configuration.GetSection(GeminiRecommendationOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ApiKey),
                "Gemini recommendation API key is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Model),
                "Gemini recommendation model is required.")
            .Validate(
                options => options.TimeoutSeconds is > 0 and <= 120,
                "Gemini recommendation timeout must be between 1 and 120 seconds.")
            .ValidateOnStart();

        services.AddScoped<ITrackRecommendationService, TrackRecommendationService>();
        services.AddScoped<IRecommendationUserContextService,RecommendationUserContextService>();
        services.AddScoped<IAssistantMessageService, AssistantMessageService>();

        services.AddHttpClient<IRecommendationModelClient, GeminiRecommendationModelClient>()
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<GeminiRecommendationOptions>>()
                    .Value;

                client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

        // IOfferedTrackReader is supplied by the Admission catalog module.
    }

    // for worker
    public static IServiceCollection AddAIWorkerModule(this IServiceCollection services)
    {
        services.AddScoped<ITrackRecommendationService, TrackRecommendationService>();
        services.AddHttpClient<IRecommendationModelClient, GeminiRecommendationModelClient>()
            .ConfigureHttpClient((serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<GeminiRecommendationOptions>>()
                    .Value;

                client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

        services.AddDocumentVerificationServices();

        return services;
    }

    // Document verification (Seif). Only the Worker consumes VerifyApplicantDocumentV1, so this
    // is wired into AddAIWorkerModule rather than the API-facing AddAIModule.
    private static void AddDocumentVerificationServices(this IServiceCollection services)
    {
        services.AddOptions<GeminiOptions>()
            .BindConfiguration(GeminiOptions.SectionName);

        services.AddScoped<IDocumentVerificationFileReader, DocumentVerificationFileReader>();
        services.AddScoped<IDocumentVerificationContextReader, DocumentVerificationContextReader>();
        services.AddScoped<IDocumentVerificationService, DocumentVerificationService>();

        services.AddHttpClient<IDocumentVerificationGeminiClient, DocumentVerificationGeminiClient>(
            (serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<GeminiOptions>>()
                    .Value;

                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(60);
            });
    }
}
