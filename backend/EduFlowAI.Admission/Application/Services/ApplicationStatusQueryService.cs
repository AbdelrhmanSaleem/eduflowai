using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Admission.Application.Extensions;
using EduFlowAI.Admission.Application.Interfaces.Repositories;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Application.Models;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Shared.Kernel.Common.Pagination;
using Microsoft.EntityFrameworkCore;

namespace EduFlowAI.Admission.Application.Services
{
    public class ApplicationStatusQueryService : IApplicationStatusQueryService
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IGenericRepository<SimulatedStageResult> _stageRepository;
        private readonly ITimelineCalculator _timelineCalculator;

        public ApplicationStatusQueryService(IApplicationRepository applicationRepository,
            IGenericRepository<SimulatedStageResult> stageRepository,
            ITimelineCalculator timelineCalculator)
        {
            _applicationRepository = applicationRepository;
            _stageRepository = stageRepository;
            _timelineCalculator = timelineCalculator;
        }

        public async Task<ApplicationStatusDto?> GetApplicationStatusAsync(Guid applicationId)
        {
            var application = await _applicationRepository.GetFirstOrDefaultAsync(app => app.Id == applicationId);
            if(application == null)
            {
                return null;
            }

            return new ApplicationStatusDto
            {
                ApplicationId = applicationId,
                CurrentStatus = application.Status.ToString(),
                LastUpdatedAt = application.UpdatedAt,
                StatusMessage = "Status retrieved successfully."
            };
        }

        public async Task<ApplicationStatusDto?> GetCurrentApplicationStatusForApplicantAsync(string applicantUserId, CancellationToken cancellationToken = default)
        {
            // Newest live application; a withdrawn one only when there is nothing else.
            var application =
                await _applicationRepository.GetFirstOrDefaultAsync(
                    predicate: app => app.ApplicantUserId == applicantUserId
                                   && app.Status != ApplicationStatus.Withdrawn,
                    include: query => query.OrderByDescending(app => app.CreatedAt))
                ?? await _applicationRepository.GetFirstOrDefaultAsync(
                    predicate: app => app.ApplicantUserId == applicantUserId,
                    include: query => query.OrderByDescending(app => app.CreatedAt));

            if(application == null)
            {
                return null;
            }

            return new ApplicationStatusDto
            {
                ApplicationId = application.Id,
                CurrentStatus = application.Status.ToString(),
                LastUpdatedAt = application.UpdatedAt,
                StatusMessage = "Status retrieved successfully."
            };
        }

        public async Task<ApplicationDashboardSummaryDto?> GetDashboardSummaryAsync(Guid applicationId)
        {
            // 1. Fetch the application with its related Cycle and EligibilityResult using the generic repository's include parameter
            var application = await _applicationRepository.GetFirstOrDefaultAsync(
                predicate: app => app.Id == applicationId,
                include: query => query.Include(a => a.Cycle)
                                 .Include(a => a.EligibilityResult)
                                 .Include(a => a.AcceptedTrackBranchOffering)
                                    .ThenInclude(tbo => tbo!.Track)
                                 .Include(a => a.AcceptedTrackBranchOffering)
                                    .ThenInclude(tbo => tbo!.Branch)
                                 .Include(a => a.Preferences)
                                    .ThenInclude(p => p.TrackBranchOffering)
                                        .ThenInclude(tbo => tbo.Track)
                                 .Include(a => a.Preferences)
                                    .ThenInclude(p => p.TrackBranchOffering)
                                        .ThenInclude(tbo => tbo.Branch)
                                 .Include(a => a.SimulatedStageResults)
            );
            if (application == null)
            {
                return null;
            }

            // Determine eligibility text based on the Passed boolean
            string eligibilityText = application.EligibilityResult != null
                ? (application.EligibilityResult.Passed ? "Eligible" : "Not Eligible")
                : "Pending";

            // Fetch all simulated stages for this application from the database
            //var stages = await _stageRepository.GetAllAsync(s => s.ApplicationId == applicationId) ?? new List<SimulatedStageResult>();
            var stages = application.SimulatedStageResults?.ToList() ?? new List<SimulatedStageResult>();

            // Map the domain status to the frontend timeline stages dynamically
            var timeline = _timelineCalculator.Calculate(application.Status, stages, application.EligibilityResult);

            // 2. Final Result & Waitlist Logic
            string? trackName = null;
            string? branchName = null;
            int? waitlistPosition = null;

            if (application.Status == ApplicationStatus.Admitted && application.AcceptedTrackBranchOffering != null)
            {
                trackName = application.AcceptedTrackBranchOffering.Track?.Name;
                branchName = application.AcceptedTrackBranchOffering.Branch?.Name;
            }
            else if (application.Status == ApplicationStatus.Waitlisted && application.Preferences != null)
            {
                // Find their highest preference to show on the waitlist card
                var topPref = application.Preferences.OrderBy(p => p.Rank).FirstOrDefault();
                if (topPref != null)
                {
                    trackName = topPref.TrackBranchOffering?.Track?.Name;
                    branchName = topPref.TrackBranchOffering?.Branch?.Name;

                    // 2.1 Calculate Waitlist Rank accurately
                    var targetOfferingId = topPref.TrackBranchOfferingId;
                    var targetTrackId = topPref.TrackBranchOffering!.TrackId;

                    // Fetch all waitlisted apps for the same cycle
                    var allWaitlisted = await _applicationRepository.GetAllAsync(
                        a => a.CycleId == application.CycleId && a.Status == ApplicationStatus.Waitlisted,
                        include: q => q.Include(a => a.Preferences).Include(a => a.SimulatedStageResults)
                    );

                    int higherScoringApplicants = 0;
                    foreach (var otherApp in allWaitlisted!.Where(a => a.Id != application.Id))
                    {
                        var otherTopPref = otherApp.Preferences?.OrderBy(p => p.Rank).FirstOrDefault();
                        if (otherTopPref != null && otherTopPref.TrackBranchOfferingId == targetOfferingId)
                        {
                            // If another waitlisted applicant is aiming for the exact same offering, compare them
                            if (IsOtherApplicantBetter(otherApp, application, targetTrackId))
                            {
                                higherScoringApplicants++;
                            }
                        }
                    }

                    // Your rank is the number of people better than you + 1
                    waitlistPosition = higherScoringApplicants + 1;
                }
            }

            return new ApplicationDashboardSummaryDto
            {
                ApplicationId = application.Id,
                IntakeName = application.Cycle?.Label ?? "Unknown Intake",
                SubmittedAt = application.SubmittedAt,
                LastUpdatedAt = application.UpdatedAt,
                CurrentStatus = application.Status.ToString(),
                EligibilityResult = eligibilityText,
                StatusMessage = application.Status.GetDisplayMessage(),

                TrackName = trackName,
                BranchName = branchName,
                WaitlistPosition = waitlistPosition,

                // Bind the newly calculated timeline fields
                TimelineProgressPercentage = timeline.Percentage,
                ApplicationPhaseStatus = timeline.AppStatus,
                EligibilityPhaseStatus = timeline.EligStatus,
                VerificationPhaseStatus = timeline.VerifStatus,
                EnglishIqPhaseStatus = timeline.EngStatus,
                TechnicalPhaseStatus = timeline.TechStatus,
                InterviewPhaseStatus = timeline.IntStatus,
                FinalResultPhaseStatus = timeline.FinalStatus
            };
        }

        public async Task<ApplicationDetailsDto?> GetApplicationDetailsAsync(Guid applicationId, string applicantUserId, CancellationToken cancellationToken = default)
        {
            // Fetch application with its related preferences, ensuring the user owns it
            var application = await _applicationRepository.GetFirstOrDefaultAsync(
                predicate: app => app.Id == applicationId && app.ApplicantUserId == applicantUserId,
                include: query => query.Include(a => a.Cycle)   // Required to fetch Cycle Label and Deadline
                                       .Include(a => a.Preferences)
                                       .ThenInclude(p => p.TrackBranchOffering)     // Required to extract TrackId and BranchId
            );

            if (application == null)
                return null;

            // Map preferences entity to DTO
            var preferencesDto = application.Preferences?
                .OrderBy(p => p.Rank)
                .Select(p => new PreferenceDto(p.TrackBranchOffering.TrackId,
                p.TrackBranchOffering.BranchId,
                p.Rank))
                .ToList() ?? new List<PreferenceDto>();

            return new ApplicationDetailsDto(
                application.Id,
                application.ApplicantUserId,
                application.CycleId,
                application.Cycle?.Label ?? string.Empty, // Map Label to CycleName
                application.Cycle?.DeadlineUtc ?? DateTimeOffset.MinValue, // Map DeadlineUtc safely
                application.Status.ToString(),
                application.CreatedAt,
                application.UpdatedAt,
                preferencesDto
            );
        }

        public async Task<PaginatedResult<ApplicationListDto>> GetMyApplicationsAsync(string applicantUserId,
            QueryParameters queryParams, CancellationToken cancellationToken = default)
        {
            // 1. Fetch all applications for the specific user and include the Cycle to retrieve the IntakeName
            var applications = await _applicationRepository.GetAllAsync(
                predicate: a => a.ApplicantUserId == applicantUserId,
                include: query => query.Include(a => a.Cycle)
                                       .ThenInclude(c => c.Program)
            );

            applications ??= new List<Domain.Entities.Application>();

            // 2. Calculate pagination metrics
            var totalCount = applications.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)queryParams.PageSize);

            // 3. Apply sorting and pagination in-memory
            var paginatedApps = applications
                .OrderByDescending(a => a.CreatedAt)
                .Skip((queryParams.Page - 1) * queryParams.PageSize)
                .Take(queryParams.PageSize)
                .ToList();

            // 4. Map the paginated entities to the lightweight DTO
            var data = paginatedApps.Select(a => new ApplicationListDto
            {
                Id = a.Id,
                ProgramName = a.Cycle?.Program?.Name ?? string.Empty,
                IntakeName = a.Cycle?.Label ?? string.Empty,
                Status = a.Status.ToString(),
                SubmittedAt = a.SubmittedAt
            }).ToList();

            // 5. Construct and return the unified paginated result
            return new PaginatedResult<ApplicationListDto>
            {
                Data = data,
                CurrentPage = queryParams.Page,
                PageSize = queryParams.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                Filters = queryParams
            };
        }

        public async Task<EligibilityDetailsDto?> GetEligibilityDetailsAsync(Guid applicationId, string applicantUserId, CancellationToken cancellationToken = default)
        {
            // 1. Fetch the application to verify ownership and include the eligibility result
            var application = await _applicationRepository.GetFirstOrDefaultAsync(
                predicate: a => a.Id == applicationId && a.ApplicantUserId == applicantUserId,
                include: q => q.Include(a => a.EligibilityResult)
            );
            // 2. Return null if application is not found, unauthorized, or has no eligibility result
            if (application == null || application.EligibilityResult == null)
            {
                return null;
            }

            var eligibility = application.EligibilityResult;
            var failureReasons = new List<string>();

            // 3. Safely deserialize the JSON string into a List of strings
            if (!string.IsNullOrWhiteSpace(eligibility.FailureReasonsJson))
            {
                try
                {
                    var reasonsObjects = System.Text.Json.JsonSerializer.Deserialize<List<EligibilityFailureReason>>(eligibility.FailureReasonsJson);

                    if (reasonsObjects != null)
                    {
                        // Map the object properties to a list of strings (Messages) for the DTO
                        failureReasons = reasonsObjects.Select(r => r.Message).ToList();
                    }
                }
                catch
                {       // Fallback to an empty list in case of JSON parsing errors
                    failureReasons = new List<string>();
                }
            }

            // 4. Map to DTO and return
            return new EligibilityDetailsDto
            {
                Passed = eligibility.Passed,
                EvaluatedAt = eligibility.EvaluatedAt,
                FailureReasons = failureReasons
            };
        }

        public async Task<EnrollmentChecklistDto?> GetEnrollmentChecklistAsync(Guid applicationId, string applicantUserId, CancellationToken cancellationToken = default)
        {
            // 1. Fetch application, ensuring it belongs to the user, and include checklist data
            var application = await _applicationRepository.GetFirstOrDefaultAsync(
                predicate: app => app.Id == applicationId && app.ApplicantUserId == applicantUserId,
                include: query => query.Include(a => a.EnrollmentTasks)
                                       .ThenInclude(t => t.TaskLookup)
            );
            if (application == null)
                return null;

            // 2. Handle null collections gracefully
            var enrollmentTasks = application.EnrollmentTasks ?? new List<ApplicationEnrollmentTask>();

            // 3. Map the tasks to DTOs and sort by DisplayOrder
            var taskDtos = enrollmentTasks
                .OrderBy(t => t.TaskLookup?.DisplayOrder ?? 0)
                .Select(t => new EnrollmentTaskItemDto
                {
                    Id = t.Id,
                    Title = t.TaskLookup?.Title ?? string.Empty,
                    // Enum Status is converted to string for the frontend UI logic
                    Status = t.Status.ToString(),
                    // Enum TaskType is converted to string to dictate UI components (e.g., Payment, Signature)
                    TaskType = t.TaskLookup?.TaskType.ToString() ?? string.Empty,
                    SubtextMessage = t.Message,
                    ActionUrl = t.ActionUrl
                })
                .ToList();

            // 4. Calculate progress metrics based on the Completed status
            int totalTasks = taskDtos.Count;
            int completedTasks = enrollmentTasks.Count(t => t.Status == Domain.Enums.EnrollmentTaskStatus.Completed);

            // 5. Return the constructed DTO wrapper
            return new EnrollmentChecklistDto
            {
                CompletedTasksCount = completedTasks,
                TotalTasksCount = totalTasks,
                Tasks = taskDtos
            };
        }

        // ========================== Helper Methods ===============================
        /// <summary>
        /// Helper Method matching exactly AllocationService Tie-Breakers.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="current"></param>
        /// <param name="trackId"></param>
        /// <returns></returns>
        private bool IsOtherApplicantBetter(Domain.Entities.Application other, Domain.Entities.Application current, Guid trackId)
        {
            decimal GetScore(Domain.Entities.Application app, SelectionStage stage, Guid? tId = null) =>
                app.SimulatedStageResults?.FirstOrDefault(s => s.Stage == stage && s.TrackId == tId)?.Score ?? 0;

            // Scores for Current
            var currProg = GetScore(current, SelectionStage.ProgrammingExam, trackId);
            var currTech = GetScore(current, SelectionStage.TechnicalInterview, trackId);
            var currTotal = (GetScore(current, SelectionStage.EnglishExamAndIq) * 0.5m) +
                            (currProg * 1.0m) + (currTech * 1.0m) +
                            (GetScore(current, SelectionStage.SoftSkillsInterview) * 1.0m);
            var currPrefRank = current.Preferences?.FirstOrDefault(p => p.TrackBranchOffering?.TrackId == trackId)?.Rank ?? 1;

            // Scores for Other
            var otherProg = GetScore(other, SelectionStage.ProgrammingExam, trackId);
            var otherTech = GetScore(other, SelectionStage.TechnicalInterview, trackId);
            var otherTotal = (GetScore(other, SelectionStage.EnglishExamAndIq) * 0.5m) +
                             (otherProg * 1.0m) + (otherTech * 1.0m) +
                             (GetScore(other, SelectionStage.SoftSkillsInterview) * 1.0m);
            var otherPrefRank = other.Preferences?.FirstOrDefault(p => p.TrackBranchOffering?.TrackId == trackId)?.Rank ?? 1;

            // Apply exact Tie-Breakers exactly like AllocationService
            if (otherTotal != currTotal) return otherTotal > currTotal;
            if (otherProg != currProg) return otherProg > currProg;
            if (otherTech != currTech) return otherTech > currTech;

            // Smaller rank number is better (Preference 1 beats Preference 2)
            if (otherPrefRank != currPrefRank) return otherPrefRank < currPrefRank;

            // Final tie-breaker: Earlier submission wins
            return other.SubmittedAt < current.SubmittedAt;
        }
    }
}
