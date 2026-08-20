using System.Linq;
using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.AI.Application.DTOs;
using EduFlowAI.AI.Application.Interfaces;
using EduFlowAI.AI.Presentation.DTOs;
using EduFlowAI.AI.Presentation.Services;
using EduFlowAI.Documents.Application.DTOs;
using EduFlowAI.Documents.Application.Interfaces;
using EduFlowAI.Shared.Kernel.Common;
using EduFlowAI.Shared.Kernel.Common.Pagination;

namespace EduFlowAI.AI.Tests;

public class AssistantMessageServiceTests
{
    [Fact]
    public async Task KnowledgeIntent_ReturnsRagResult()
    {
        var service = CreateService(
            intent: "knowledge",
            recommendationService:
                new FakeTrackRecommendationService());

        var response = await service.HandleAsync(
            new AssistantMessageRequest(
                "What tracks are available?",
                null,
                "en"),
            Guid.Empty,
            false);

        var result = Assert.Single(response.Results);

        Assert.Equal("knowledge", result.Intent);
        Assert.Equal("Admission Information", result.Title);
        Assert.Equal("Test answer", result.Content);
        Assert.Contains("Test Source", result.Sources);
        Assert.False(response.RequiresClarification);
    }

    // The answer language must follow the language the applicant wrote in, not just the UI toggle -
    // an Arabic question stays Arabic even when the request's Language is null or "en".
    [Theory]
    [InlineData("ما هي متطلبات القبول؟", null, "ar")]       // Arabic message, no UI language -> Arabic
    [InlineData("ما هي متطلبات القبول؟", "en", "ar")]       // Arabic message, UI says English -> still Arabic
    [InlineData("What are the requirements?", "ar", "en")]  // English message, UI says Arabic -> English
    [InlineData("12345", "ar", "ar")]                       // no letters -> fall back to the UI language
    public async Task Answer_language_follows_the_message_not_just_the_ui_toggle(
        string message,
        string? requestLanguage,
        string expected)
    {
        var rag = new FakeRagAnswerService();
        var service = CreateService(
            intent: "knowledge",
            recommendationService: new FakeTrackRecommendationService(),
            rag: rag);

        await service.HandleAsync(
            new AssistantMessageRequest(message, null, requestLanguage),
            Guid.Empty,
            false);

        Assert.Equal(expected, rag.LastLanguage);
    }

    // The router reads code-switched messages the letter-count heuristic gets wrong, so it wins.
    [Fact]
    public async Task Router_detected_language_wins_over_the_letter_count_heuristic()
    {
        var rag = new FakeRagAnswerService();
        var service = CreateService(
            intent: "knowledge",
            recommendationService: new FakeTrackRecommendationService(),
            rag: rag,
            routerLanguage: "ar");

        // The heuristic scores this "en" (more Latin letters than Arabic); the router's "ar" must win.
        await service.HandleAsync(
            new AssistantMessageRequest("اني Product Manager", null, "en"),
            Guid.Empty,
            false);

        Assert.Equal("ar", rag.LastLanguage);
    }

    // A stray/unsupported language from the router is ignored - the message heuristic still decides.
    [Fact]
    public async Task Invalid_router_language_falls_back_to_the_message_heuristic()
    {
        var rag = new FakeRagAnswerService();
        var service = CreateService(
            intent: "knowledge",
            recommendationService: new FakeTrackRecommendationService(),
            rag: rag,
            routerLanguage: "fr");   // not "ar"/"en" -> ignored

        await service.HandleAsync(
            new AssistantMessageRequest("ما هي متطلبات القبول؟", null, "en"),
            Guid.Empty,
            false);

        Assert.Equal("ar", rag.LastLanguage);
    }

    [Fact]
    public async Task DocumentStatusIntent_AuthenticatedUser_ReturnsNarratedDocumentStatus()
    {
        var service = CreateService(
            intent: "document_status",
            recommendationService:
                new FakeTrackRecommendationService());

        var response = await service.HandleAsync(
            new AssistantMessageRequest(
                "Did you receive my documents?",
                null,
                "en"),
            Guid.NewGuid(),
            isAuthenticated: true);

        var result = Assert.Single(response.Results);

        Assert.Equal("document_status", result.Intent);
        Assert.Equal("Document Status", result.Title);
        Assert.Equal("Test document status", result.Content);
    }

    [Fact]
    public async Task RecommendWithAvailableData_ReturnsCompletedWithoutAskingQuestions()
    {
        var recommendationService =
            new FakeTrackRecommendationService();

        var service = CreateService(
            intent: "recommendation",
            recommendationService);

        var request = new AssistantMessageRequest(
            Message: "Recommend based on what you know.",
            SessionId: null,
            Language: "en",
            Recommendation:
                new RecommendationQuestionnaireProgressDto
                {
                    Interests = ["Backend Development"]
                },
            RecommendWithAvailableData: true);

        var response = await service.HandleAsync(
            request,
            Guid.Empty,
            false);

        var result = Assert.Single(response.Results);

        Assert.Equal("recommendation", result.Intent);
        Assert.Equal("Recommended Tracks", result.Title);
        Assert.Equal("completed", result.Metadata["state"]);
        Assert.Equal(true, result.Metadata["basedOnAvailableData"]);
        Assert.Equal(true, result.Metadata["advisory"]);

        Assert.NotNull(recommendationService.LastQuestionnaire);

        Assert.Contains(
            "Backend Development",
            recommendationService.LastQuestionnaire.Interests);

        Assert.Empty(
            recommendationService.LastQuestionnaire.CareerGoals);

        Assert.Empty(
            recommendationService.LastQuestionnaire.Skills);
    }

    [Fact]
    public async Task AuthenticatedUser_WithProfileMajor_DoesNotAskForMajor()
    {
        var service = CreateService(
            intent: "recommendation",
            recommendationService:
                new FakeTrackRecommendationService(),
            userContext:
                new RecommendationUserContextDto
                {
                    Major = "Computer Science",
                    Faculty = "Computers and Information",
                    University = "Test University",
                    DegreeLevel = "Bachelor",
                    GraduationYear = 2025,
                    CumulativeGrade = "VeryGood",
                    HasProfileData = true
                });

        var response = await service.HandleAsync(
            new AssistantMessageRequest(
                Message: "Recommend me a track.",
                SessionId: null,
                Language: "en",
                Recommendation: null,
                RecommendWithAvailableData: false),
            Guid.NewGuid(),
            true);

        var result = Assert.Single(response.Results);

        Assert.Equal("recommendation", result.Intent);
        Assert.Equal(
            "collecting_answers",
            result.Metadata["state"]);
        Assert.Equal(
            "careerGoals",
            result.Metadata["questionKey"]);
        Assert.Equal(1, result.Metadata["questionNumber"]);
        Assert.Equal(3, result.Metadata["totalQuestions"]);

        var missingContext =
            Assert.IsAssignableFrom<IReadOnlyList<string>>(
                result.Metadata["missingContext"]);

        Assert.DoesNotContain("major", missingContext);
    }

    [Fact]
    public async Task AuthenticatedUser_WithoutProfileMajor_AsksForMajorFirst()
    {
        var service = CreateService(
            intent: "recommendation",
            recommendationService:
                new FakeTrackRecommendationService(),
            userContext:
                new RecommendationUserContextDto
                {
                    HasProfileData = false
                });

        var response = await service.HandleAsync(
            new AssistantMessageRequest(
                Message: "Recommend me a track.",
                SessionId: null,
                Language: "en",
                Recommendation: null,
                RecommendWithAvailableData: false),
            Guid.NewGuid(),
            true);

        var result = Assert.Single(response.Results);

        Assert.Equal("recommendation", result.Intent);
        Assert.Equal(
            "collecting_answers",
            result.Metadata["state"]);
        Assert.Equal("major", result.Metadata["questionKey"]);
        Assert.Equal(1, result.Metadata["questionNumber"]);
        Assert.Equal(4, result.Metadata["totalQuestions"]);

        var missingContext =
            Assert.IsAssignableFrom<IReadOnlyList<string>>(
                result.Metadata["missingContext"]);

        Assert.Contains("major", missingContext);
    }

    [Fact]
    public async Task AuthenticatedUser_ProfileData_IsIncludedInRecommendationQuestionnaire()
    {
        var recommendationService =
            new FakeTrackRecommendationService();

        var service = CreateService(
            intent: "recommendation",
            recommendationService,
            new RecommendationUserContextDto
            {
                Major = "Computer Science",
                Faculty = "Computers and Information",
                University = "Test University",
                DegreeLevel = "Bachelor",
                GraduationYear = 2025,
                CumulativeGrade = "VeryGood",
                HasProfileData = true
            });

        var response = await service.HandleAsync(
            new AssistantMessageRequest(
                Message: "Recommend based on my available information.",
                SessionId: null,
                Language: "en",
                Recommendation: null,
                RecommendWithAvailableData: true),
            Guid.NewGuid(),
            true);

        var result = Assert.Single(response.Results);

        Assert.Equal("completed", result.Metadata["state"]);

        var questionnaire =
            Assert.IsType<RecommendationQuestionnaireDto>(
                recommendationService.LastQuestionnaire);

        Assert.Equal(
            "Computer Science",
            questionnaire.Major);

        Assert.NotNull(questionnaire.AdditionalContext);

        Assert.Contains(
            "Faculty: Computers and Information",
            questionnaire.AdditionalContext);

        Assert.Contains(
            "University: Test University",
            questionnaire.AdditionalContext);

        Assert.Contains(
            "Degree level: Bachelor",
            questionnaire.AdditionalContext);

        Assert.Contains(
            "Graduation year: 2025",
            questionnaire.AdditionalContext);

        Assert.Contains(
            "Cumulative grade: VeryGood",
            questionnaire.AdditionalContext);
    }

    // The router's English query is what retrieval embeds.
    [Fact]
    public async Task Router_search_query_is_handed_to_retrieval()
    {
        var rag = new FakeRagAnswerService();
        var service = CreateService(
            intent: "knowledge",
            recommendationService: new FakeTrackRecommendationService(),
            rag: rag,
            routerSearchQuery: "tracks offered at Alexandria branch");

        await service.HandleAsync(
            new AssistantMessageRequest("ايه التراكات المتاحة في الإسكندرية", null, "ar"),
            Guid.Empty,
            false);

        Assert.Equal("tracks offered at Alexandria branch", rag.LastSearchQuery);
    }

    [Fact]
    public async Task No_router_search_query_leaves_retrieval_on_its_own_query()
    {
        var rag = new FakeRagAnswerService();
        var service = CreateService(
            intent: "knowledge",
            recommendationService: new FakeTrackRecommendationService(),
            rag: rag);

        await service.HandleAsync(
            new AssistantMessageRequest("What tracks are available?", null, "en"),
            Guid.Empty,
            false);

        Assert.Null(rag.LastSearchQuery);
    }

    // A failing subsystem costs that one answer, not the whole reply.
    [Theory]
    [InlineData("en", "Something Went Wrong")]
    [InlineData("ar", "تعذّر إتمام الطلب")]
    public async Task A_failing_intent_degrades_gracefully_instead_of_throwing(
        string language,
        string expectedTitle)
    {
        var rag = new FakeRagAnswerService { Throw = true };
        var service = CreateService(
            intent: "knowledge",
            recommendationService: new FakeTrackRecommendationService(),
            rag: rag);

        var response = await service.HandleAsync(
            new AssistantMessageRequest(
                language == "ar" ? "ما هي المسارات المتاحة؟" : "What tracks are available?",
                null,
                language),
            Guid.Empty,
            false);

        var result = Assert.Single(response.Results);

        Assert.Equal(expectedTitle, result.Title);
        Assert.Equal(true, result.Metadata["failed"]);
        Assert.False(response.RequiresClarification);
    }

    // Switching intent used to reach a narrator that had never seen the conversation.
    [Fact]
    public async Task Document_status_is_told_when_the_conversation_is_already_underway()
    {
        var narrator = new FakeDocumentStatusNarrator();
        var service = CreateService(
            intent: "document_status",
            recommendationService: new FakeTrackRecommendationService(),
            narrator: narrator);

        var first = await service.HandleAsync(
            new AssistantMessageRequest("Did you receive my documents?", null, "en"),
            Guid.NewGuid(),
            isAuthenticated: true);

        Assert.False(narrator.LastIsContinuingConversation);

        await service.HandleAsync(
            new AssistantMessageRequest("And my transcript?", first.SessionId, "en"),
            Guid.NewGuid(),
            isAuthenticated: true);

        Assert.True(narrator.LastIsContinuingConversation);
    }

    private static AssistantMessageService CreateService(
        string intent,
        FakeTrackRecommendationService recommendationService,
        RecommendationUserContextDto? userContext = null,
        FakeRagAnswerService? rag = null,
        string? routerLanguage = null,
        string? routerSearchQuery = null,
        FakeDocumentStatusNarrator? narrator = null)
    {
        return new AssistantMessageService(
            new FakeIntentRouter(intent, routerLanguage, routerSearchQuery),
            rag ?? new FakeRagAnswerService(),
            new FakeSessionManager(),
            recommendationService,
            new FakeApplicationStatusQueryService(),
            new FakeApplicantDocumentService(),
            narrator ?? new FakeDocumentStatusNarrator(),
            new FakeApplicationStatusNarrator(),
            new FakeRecommendationUserContextService(
                userContext ??
                new RecommendationUserContextDto()));
    }

    private sealed class FakeIntentRouter(
        string intent = "knowledge",
        string? language = null,
        string? searchQuery = null)
        : IIntentRouter
    {
        public Task<IntentClassificationDto> ClassifyAsync(
            string userQuestion,
            List<ConversationTurnDto> context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new IntentClassificationDto
                {
                    PrimaryIntent = intent,
                    Confidence = 0.95m,
                    RoutedTo = GetRoute(intent),
                    Language = language,
                    SearchQuery = searchQuery,
                    RequiresClarification = false,
                    Intents =
                    [
                        new DetectedIntentDto
                        {
                            Intent = intent,
                            Confidence = 0.95m,
                            RoutedTo = GetRoute(intent)
                        }
                    ]
                });
        }

        public string GetClarificationMessage(string language)
        {
            return "Clarify";
        }

        private static string GetRoute(string intent)
        {
            return intent switch
            {
                "knowledge" => "knowledge_rag",
                "recommendation" => "recommendations_agent",
                "status" or "application_status" => "status_service",
                "document_status" => "document_status_service",
                _ => "unknown"
            };
        }
    }

    private sealed class FakeRagAnswerService : IRagAnswerService
    {
        public string? LastLanguage { get; private set; }

        public string? LastSearchQuery { get; private set; }

        public bool Throw { get; set; }

        public Task<RagAnswerDto> AnswerWithContextAsync(
            string userQuestion,
            IReadOnlyList<ConversationTurnDto> context,
            string language,
            string? searchQuery = null,
            CancellationToken cancellationToken = default)
        {
            LastLanguage = language;
            LastSearchQuery = searchQuery;

            if (Throw)
                throw new InvalidOperationException("knowledge subsystem is down");

            return Task.FromResult(
                new RagAnswerDto
                {
                    Answer = "Test answer",
                    Sources = ["Test Source"]
                });
        }
    }

    // Stateful on purpose: continuation is derived from stored history.
    private sealed class FakeSessionManager : ISessionManager
    {
        private readonly Dictionary<string, ChatSessionDto> _sessions = new();

        public ChatSessionDto CreateSession(string userId)
        {
            var session = new ChatSessionDto
            {
                SessionId = Guid.NewGuid().ToString(),
                UserId = userId
            };

            _sessions[session.SessionId] = session;

            return session;
        }

        public ChatSessionDto? GetSession(string sessionId)
        {
            return _sessions.TryGetValue(sessionId, out var session) ? session : null;
        }

        public bool AddTurn(string sessionId, ConversationTurnDto turn)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return false;

            session.History.Add(turn);

            return true;
        }

        public List<ConversationTurnDto> GetHistory(string sessionId, int lastN = 3)
        {
            if (!_sessions.TryGetValue(sessionId, out var session))
                return [];

            return session.History
                .Skip(Math.Max(0, session.History.Count - lastN))
                .ToList();
        }

        public void EndSession(string sessionId)
        {
            _sessions.Remove(sessionId);
        }
    }

    private sealed class FakeTrackRecommendationService
        : ITrackRecommendationService
    {
        public RecommendationQuestionnaireDto? LastQuestionnaire
        {
            get;
            private set;
        }

        public Task<TrackRecommendationResultDto> RecommendAsync(
            RecommendationQuestionnaireDto questionnaire,
            CancellationToken cancellationToken = default)
        {
            LastQuestionnaire = questionnaire;

            return Task.FromResult(
                new TrackRecommendationResultDto
                {
                    Recommendations =
                    [
                        new RecommendedTrackResultDto
                        {
                            TrackId = Guid.NewGuid(),
                            TrackName = ".NET Development",
                            Rank = 1,
                            Reason =
                                "Matches the available backend-development interest."
                        }
                    ],
                    UsedFallback = false,
                    AdvisoryNotice =
                        "These recommendations are advisory only."
                });
        }
    }

    private sealed class FakeApplicationStatusQueryService
        : IApplicationStatusQueryService
    {
        public Task<ApplicationDetailsDto?> GetApplicationDetailsAsync(Guid applicationId, string applicantUserId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ApplicationDetailsDto?>(null);
        }

        public Task<ApplicationStatusDto?> GetApplicationStatusAsync(
            Guid applicationId)
        {
            return Task.FromResult<ApplicationStatusDto?>(null);
        }

        public Task<ApplicationStatusDto?> GetCurrentApplicationStatusForApplicantAsync(
            string applicantUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ApplicationStatusDto?>(null);
        }

        public Task<ApplicationDashboardSummaryDto?> GetDashboardSummaryAsync(Guid applicationId)
        {
            return Task.FromResult<ApplicationDashboardSummaryDto?>(null);
        }

        public Task<EligibilityDetailsDto?> GetEligibilityDetailsAsync(Guid applicationId, string applicantUserId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<EligibilityDetailsDto?>(null);
        }

        public Task<EnrollmentChecklistDto?> GetEnrollmentChecklistAsync(Guid applicationId, string applicantUserId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PaginatedResult<ApplicationListDto>> GetMyApplicationsAsync(string applicantUserId, QueryParameters queryParams, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PaginatedResult<ApplicationListDto>>(new PaginatedResult<ApplicationListDto>());
        }
    }

    private sealed class FakeApplicantDocumentService
        : IApplicantDocumentService
    {
        public Task<Result<Guid>> UploadDocumentAsync(
            UploadDocumentDto dto,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Result<IEnumerable<ApplicantDocumentDto>>>
            GetDocumentsByApplicationIdAsync(
                Guid applicationId,
                CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Result<FileDownloadDto>> DownloadDocumentAsync(
            Guid documentId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Result<IEnumerable<ApplicantDocumentDto>>>
            GetDocumentsForApplicantAsync(
                string userId,
                CancellationToken cancellationToken)
        {
            return Task.FromResult(
                Result<IEnumerable<ApplicantDocumentDto>>.Success(
                    []));
        }

        public Task<Result<RequiredDocumentsDto>> GetRequiredDocumentTypesAsync(Guid applicationId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeDocumentStatusNarrator
        : IDocumentStatusNarrator
    {
        public bool? LastIsContinuingConversation { get; private set; }

        public Task<string> NarrateAsync(
            IEnumerable<ApplicantDocumentDto> documents,
            string language,
            bool isContinuingConversation = false,
            CancellationToken cancellationToken = default)
        {
            LastIsContinuingConversation = isContinuingConversation;

            return Task.FromResult("Test document status");
        }
    }

    private sealed class FakeApplicationStatusNarrator
        : IApplicationStatusNarrator
    {
        public Task<string> NarrateAsync(
            string currentStatus,
            DateTimeOffset lastUpdatedAt,
            string language,
            bool isContinuingConversation = false,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult($"Narrated: {currentStatus}");
        }
    }

    private sealed class FakeRecommendationUserContextService(
        RecommendationUserContextDto context)
        : IRecommendationUserContextService
    {
        public Task<RecommendationUserContextDto> GetAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(context);
        }
    }
}
