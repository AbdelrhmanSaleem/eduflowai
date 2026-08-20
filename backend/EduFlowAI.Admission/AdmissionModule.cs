using EduFlowAI.Admission.Application.EligibilityRuleStrategy;
using EduFlowAI.Admission.Application.Features.Branches;
using EduFlowAI.Admission.Application.Features.Configuration.Common;
using EduFlowAI.Admission.Application.Features.Cycles;
using EduFlowAI.Admission.Application.Features.Dashboard;
using EduFlowAI.Admission.Application.Features.Offerings;
using EduFlowAI.Admission.Application.Features.Programs;
using EduFlowAI.Admission.Application.Features.Requirements;
using EduFlowAI.Admission.Application.Features.Tracks;
using EduFlowAI.Admission.Application.Interfaces;
using EduFlowAI.Admission.Application.Interfaces.EligibilityRuleStrategy;
using EduFlowAI.Admission.Application.Interfaces.Repositories;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Application.Services;
using EduFlowAI.Admission.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using EduFlowAI.Admission.Infrastructure.ExternalServices;
using EduFlowAI.Admission.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace EduFlowAI.Admission
{
    public static class AdmissionModule
    {
        public static IServiceCollection AddAdmissionModule(
            this IServiceCollection services)
        {
            services.AddAutoMapper(cfg =>
                cfg.AddMaps(
                    typeof(Application.MappingProfiles.ApplicationProfile)
                        .Assembly));

            // Shared mechanics for Admission configuration writes.
            services.AddScoped<AdmissionWriteExecutor>();
            services.AddScoped<CycleConfigurationGuard>();

            // Abdallah-owned Admission feature boundaries.
            services.AddScoped<IProgramConfigurationService, ProgramConfigurationService>();
            services.AddScoped<IProgramRequirementService, ProgramRequirementService>();
            services.AddScoped<ITrackService, TrackService>();
            services.AddScoped<IBranchService, BranchService>();
            services.AddScoped<IAdmissionCycleService, AdmissionCycleService>();
            services.AddScoped<IOfferingService, OfferingService>();
            services.AddScoped<IAdmissionDashboardService, AdmissionDashboardService>();

            // Public contracts consumed by other modules.
            services.AddScoped<IProgramRequirementReader, ProgramRequirementReader>();
            services.AddScoped<IOfferedTrackReader, OfferedTrackReader>();
            services.AddScoped<IAdmissionApplicationReader, AdmissionApplicationReader>();
            services.TryAddSingleton(TimeProvider.System);

            #region Infrastructure Layer repositories.
            services.AddScoped<IApplicationRepository, ApplicationRepository>();
            services.AddScoped(
                typeof(IGenericRepository<>),
                typeof(GenericRepository<>));
            #endregion

            #region Register Eligibility rules and services
            services.AddScoped<IEligibilityRuleStrategy, GraduationRecencyRule>();
            services.AddScoped<IEligibilityRuleStrategy, CumulativeGradeRule>();
            services.AddScoped<IEligibilityRuleStrategy, NationalityRule>();
            services.AddScoped<IEligibilityRuleStrategy, MilitaryStatusRule>();
            services.AddScoped<IEligibilityEngine, EligibilityEngine>();
            services.AddScoped<IAdmissionEligibilityService, AdmissionEligibilityService>();
            #endregion

            #region Register Services
            services.AddScoped<IApplicationService, ApplicationService>();
            services.AddScoped<IStatusTransitionCoordinator, StatusTransitionCoordinator>();
            services.AddScoped<IApplicationStatusQueryService, ApplicationStatusQueryService>();
            services.AddScoped<IApplicationAcessReader, ApplicationAcessReader>();
            services.AddScoped<IAdminOperationsService, AdminOperationsService>();
            services.AddScoped<IAssessmentSimulationService, AssessmentSimulationService>();
            services.AddScoped<IFinalRankingService, FinalRankingService>();
            services.AddScoped<ITimelineCalculator, TimelineCalculator>();
            services.AddScoped<ICycleQueryService, CycleQueryService>();
            services.AddScoped<IAllocationService, AllocationService>();
            #endregion

            #region Register External n8n Email Service
            services.AddOptions<N8nAdmissionEmailOptions>()
                .Configure<IConfiguration>(
                    (options, configuration) =>
                    {
                        configuration
                            .GetSection(N8nAdmissionEmailOptions.SectionName)
                            .Bind(options);
                    })
                .Validate(options =>
                        Uri.TryCreate(options.WebhookUrl, UriKind.Absolute, out var webhookUri) &&
                        (webhookUri.Scheme == Uri.UriSchemeHttp || webhookUri.Scheme == Uri.UriSchemeHttps),
                    "N8nAdmissionEmail:WebhookUrl must be an absolute HTTP or HTTPS URL.")
                .Validate(options =>
                        !string.IsNullOrWhiteSpace(options.WebhookSecret) &&
                        options.WebhookSecret.Length >= 16,
                    "N8nAdmissionEmail:WebhookSecret must contain at least 16 characters.")
                .Validate(options => 
                        options.TimeoutSeconds is > 0 and <= 120,
                    "N8nAdmissionEmail:TimeoutSeconds must be between 1 and 120.")
                .ValidateOnStart();

            services.AddHttpClient<IAdmissionEmailNotificationService, N8nAdmissionEmailService>
                ((serviceProvider, httpClient) =>
                {
                    var options = serviceProvider
                        .GetRequiredService<IOptions<N8nAdmissionEmailOptions>>()
                        .Value;

                    httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                });
            #endregion

            services.AddAdmissionEmailDelivery();

            return services;
        }

        // for worker
        public static IServiceCollection AddAdmissionWorkerModule(
            this IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IApplicationRepository, ApplicationRepository>();
            services.AddScoped<IStatusTransitionCoordinator, StatusTransitionCoordinator>();
            services.AddScoped<IOfferedTrackReader, OfferedTrackReader>();

            services.AddAdmissionEmailDelivery();

            return services;
        }

        private static IServiceCollection AddAdmissionEmailDelivery(
            this IServiceCollection services)
        {
            services.AddOptions<N8nAdmissionEmailOptions>()
                .Configure<IConfiguration>(
                    (options, configuration) =>
                    {
                        configuration.GetSection(N8nAdmissionEmailOptions.SectionName)
                            .Bind(options);
                    })
                .Validate(
                    options =>
                        Uri.TryCreate(options.WebhookUrl, UriKind.Absolute, out var uri) &&
                        (uri.Scheme == Uri.UriSchemeHttp ||
                         uri.Scheme == Uri.UriSchemeHttps),
                    "N8nAdmissionEmail:WebhookUrl must be a valid HTTP or HTTPS URL.")
                .Validate(
                    options =>
                        !string.IsNullOrWhiteSpace(options.WebhookSecret) &&
                        options.WebhookSecret.Length >= 16,
                    "N8nAdmissionEmail:WebhookSecret must contain at least 16 characters.")
                .Validate(
                    options =>
                        options.TimeoutSeconds is > 0 and <= 120,
                    "N8nAdmissionEmail:TimeoutSeconds must be between 1 and 120.")
                .ValidateOnStart();

            services.AddHttpClient<IAdmissionEmailNotificationService, N8nAdmissionEmailService>(
                (serviceProvider, httpClient) =>
                {
                    var options = serviceProvider.GetRequiredService<IOptions<N8nAdmissionEmailOptions>>().Value;

                    httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                }
            );

            return services;
        }
    }
}
