using AutoMapper;
using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Admission.Application.Features.Offerings;
using EduFlowAI.Admission.Application.Interfaces;
using EduFlowAI.Admission.Application.Interfaces.Repositories;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;
using EduFlowAI.Identity.Domain.Entities;
using EduFlowAI.Shared.Kernel.Common;
using Microsoft.EntityFrameworkCore;

namespace EduFlowAI.Admission.Application.Services
{
    public class ApplicationService : IApplicationService
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IGenericRepository<TrackBranchOffering> _offeringRepository;
        private readonly IGenericRepository<ApplicationPreference> _preferenceRepository;
        private readonly IGenericRepository<ApplicantProfile> _profileRepository;
        //private readonly IGenericRepository<EligibilityResult> _eligibilityResultRepository;
        private readonly IAdmissionEligibilityService _eligibilityService;
        private readonly IGenericRepository<AdmissionCycle> _cycleRepository;
        private readonly IMapper _mapper;

        public ApplicationService(IApplicationRepository applicationRepository,
            IGenericRepository<TrackBranchOffering> offeringRepository,
            IGenericRepository<ApplicationPreference> preferenceRepository,
            IGenericRepository<ApplicantProfile> profileRepository,
            //IGenericRepository<EligibilityResult> eligibilityResultRepository,
            IAdmissionEligibilityService eligibilityService,
            IGenericRepository<AdmissionCycle> cycleRepository,
            IMapper mapper)
        {
            _applicationRepository = applicationRepository;
            _offeringRepository = offeringRepository;
            _preferenceRepository = preferenceRepository;
            _profileRepository = profileRepository;
            //_eligibilityResultRepository = eligibilityResultRepository;
            _eligibilityService = eligibilityService;
            _cycleRepository = cycleRepository;
            _mapper = mapper;
        }

        public async Task<Result<ApplicationDto>> CreateDraftApplicationAsync(string applicantUserId, ApplicationRequestDto request)
        {
            if(request == null)
                return Result<ApplicationDto>.Failure(400, "Request cannot be null");

            if(string.IsNullOrWhiteSpace(applicantUserId))
                return Result<ApplicationDto>.Failure(400, "Applicant user ID cannot be null or empty");

            // 1. Validate Cycle Existence, Status, and Deadline FIRST
            var cycle = await _cycleRepository.GetFirstOrDefaultAsync(c => c.Id == request.CycleId);

            if (cycle == null)
            {
                return Result<ApplicationDto>.Failure(404, "The requested admission cycle was not found.");
            }

            if (cycle.Status != CycleStatus.Active)
            {
                return Result<ApplicationDto>.Failure(400, $"The admission cycle '{cycle.Label}' is not currently active.");
            }

            if (DateTimeOffset.UtcNow > cycle.DeadlineUtc)
            {
                // This completely prevents withdrawn applicants (or new ones) from applying after exams theoretically start
                return Result<ApplicationDto>.Failure(400, $"The application deadline for '{cycle.Label}' has passed. New applications are no longer accepted.");
            }

            // 2. Business Rule Validation: One active OR concluded application per applicant and cycle.
            var existingApplication = await _applicationRepository.GetFirstOrDefaultAsync(
                predicate: a => a.ApplicantUserId == applicantUserId
                             && a.CycleId == request.CycleId
                             && a.Status != ApplicationStatus.Withdrawn
            );
            if (existingApplication != null)
            {
                bool isTerminalRejectedState = existingApplication.Status == ApplicationStatus.EligibilityFailed ||
                                               existingApplication.Status == ApplicationStatus.DocumentRejected ||
                                               existingApplication.Status == ApplicationStatus.NotSelected ||
                                               existingApplication.Status == ApplicationStatus.Expired;

                if (isTerminalRejectedState)
                {
                    return Result<ApplicationDto>.Failure(400,
                        $"You have already applied to this admission cycle and your application was concluded with status '{existingApplication.Status}'. You cannot create a new application for the same cycle.");
                }
                else
                {
                    return Result<ApplicationDto>.Failure(400,
                        "You already have an active application for this admission cycle. Please manage it from your dashboard.");
                }
            }

            // 3. Map and set Business Rules/Domain logic
            var application = _mapper.Map<Domain.Entities.Application>(request);
            //application.Id = Guid.NewGuid();
            application.ApplicantUserId = applicantUserId;
            application.Status = ApplicationStatus.Draft;
            application.CreatedAt = DateTime.UtcNow;
            application.UpdatedAt = DateTime.UtcNow;

            // Ensure preferences get unique IDs and link to the application
            if(application.Preferences != null)
            {
                foreach(var pref in application.Preferences)
                {
                    pref.Id = Guid.NewGuid();
                    pref.ApplicationId = application.Id;
                }
            }

            // 3. Save to the database
            await _applicationRepository.AddAsync(application);
            await _applicationRepository.SaveChangesAsync();

            // 4. Map the saved entity back to a DTO to return to the Presentation layer
            var resultDto = _mapper.Map<ApplicationDto>(application);

            return Result<ApplicationDto>.Success(resultDto);
        }

        public async Task<Result<ApplicationDetailsDto>> UpdateApplicationPreferencesAsync(Guid applicationId, string applicantUserId, UpdateApplicationPreferencesDto request, CancellationToken cancellationToken = default)
        {
            #region Edge cases validations
            if (request == null || request.Preferences == null || !request.Preferences.Any())
                return Result<ApplicationDetailsDto>.Failure(400, "Preferences list cannot be empty.");

            
            var uniqueOfferingsCount = request.Preferences.Select(p => new { p.TrackId, p.BranchId }).Distinct().Count();
            if (uniqueOfferingsCount != request.Preferences.Count)
                return Result<ApplicationDetailsDto>.Failure(400, "You cannot select the same track and branch combination more than once.");

            var uniqueRanksCount = request.Preferences.Select(p => p.Rank).Distinct().Count();
            if (uniqueRanksCount != request.Preferences.Count)
                return Result<ApplicationDetailsDto>.Failure(400, "Ranks must be unique for each preference.");
            #endregion

            // 1. Fetch the application and include existing preferences
            var application = await _applicationRepository.GetFirstOrDefaultAsync(
                predicate: app => app.Id == applicationId && app.ApplicantUserId == applicantUserId,
                include: query => query.Include(a => a.Preferences)
            );
            if (application == null)
                return Result<ApplicationDetailsDto>.Failure(404, "Application not found or you are not authorized to modify it.");

            // 2. Business Rule: Only allow updates if the application is in Draft status
            if (application.Status != ApplicationStatus.Draft)
                return Result<ApplicationDetailsDto>.Failure(400, "You can only edit preferences while the application is in Draft status.");

            // 3. Extract the requested Track and Branch IDs to fetch their corresponding Offerings
            var requestedTrackIds = request.Preferences.Select(p => p.TrackId).Distinct().ToList();
            var requestedBranchIds = request.Preferences.Select(p => p.BranchId).Distinct().ToList();

            // 4. Fetch matching offerings for the current Cycle in a SINGLE query (Performance optimization - Avoids N+1)
            var availableOfferings = await _offeringRepository.GetAllAsync(
                o =>
                    o.CycleId == application.CycleId &&
                    requestedTrackIds.Contains(o.TrackId) &&
                    requestedBranchIds.Contains(o.BranchId),
                query => query
                    .Include(o => o.Cycle)
                        .ThenInclude(cycle => cycle.Program)
                    .Include(o => o.Track)
                    .Include(o => o.Branch));


            // 5. Robustly sync existing preferences instead of using .Clear()
            // First, map the incoming DTOs to the resolved database offerings
            var resolvedPreferences = new List<(TrackBranchOffering Offering, short Rank)>();

            foreach (var prefDto in request.Preferences)
            {
                var matchedOffering = availableOfferings?.FirstOrDefault(o =>
                    o.TrackId == prefDto.TrackId &&
                    o.BranchId == prefDto.BranchId);

                if (matchedOffering == null ||
                    !IsSelectablePreferenceOffering(matchedOffering))
                {
                    return Result<ApplicationDetailsDto>.Failure(400, "The selected Track and Branch combination is not available for this admission cycle.");
                }

                resolvedPreferences.Add((matchedOffering, prefDto.Rank));
            }

            // A flag to track if any actual database changes are needed
            bool hasChanges = false;

            // 6. Remove old preferences that are NO LONGER in the new request
            var newOfferingIds = resolvedPreferences.Select(r => r.Offering.Id).ToList();
            var preferencesToRemove = application.Preferences?
                .Where(p => !newOfferingIds.Contains(p.TrackBranchOfferingId))
                .ToList() ?? new List<ApplicationPreference>();

            if (preferencesToRemove.Any())
            {
                hasChanges = true;
                foreach (var prefToRemove in preferencesToRemove)
                {
                    // 1. MUST remove from the parent collection to prevent Change Tracker confusion
                    application.Preferences?.Remove(prefToRemove);
                    // 2. Explicitly delete from the database
                    _preferenceRepository.Delete(prefToRemove);
                }
            }
            
            foreach (var item in resolvedPreferences)
            {
                var existingPref = application.Preferences?
                    .FirstOrDefault(p => p.TrackBranchOfferingId == item.Offering.Id);

                if(existingPref != null)
                {
                    // Only flag as changed and update if the rank actually changed!
                    // This completely avoids the ConcurrencyException on identical double-submits.
                    if (existingPref.Rank != item.Rank)
                    {
                        existingPref.Rank = item.Rank;
                        existingPref.UpdatedAt = DateTimeOffset.UtcNow; // Unified to DateTime
                        hasChanges = true;
                    }
                }
                else
                {
                    // Add as a completely new preference
                    var newPref = new ApplicationPreference
                    {
                        // Id = Guid.NewGuid(), --> XX => Don't set the ID manually; let EF Core handle it to change the entity state to Added.
                        // This avoids the ConcurrencyException on double-submits.
                        ApplicationId = application.Id,
                        TrackBranchOfferingId = item.Offering.Id,
                        Rank = item.Rank,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };

                    application.Preferences?.Add(newPref);
                    hasChanges = true;
                }
            }

            // 8. Save changes to the database ONLY if something was actually modified
            if (hasChanges)
            {
                // Update the timestamp unconditionally as EF Core handles xmin automatically
                application.UpdatedAt = DateTimeOffset.UtcNow;
                await _applicationRepository.SaveChangesAsync();
            }

            // 9. Map to response DTO
            var preferencesDto = request.Preferences.OrderBy(p => p.Rank).ToList();
            var resultDto = new ApplicationDetailsDto(
                application.Id,
                application.ApplicantUserId,
                application.CycleId,
                application.Cycle?.Label ?? string.Empty,
                application.Cycle?.DeadlineUtc ?? DateTimeOffset.MinValue,
                application.Status.ToString(),
                application.CreatedAt,
                application.UpdatedAt,
                preferencesDto
            );

            return Result<ApplicationDetailsDto>.Success(resultDto, 200, "Preferences updated successfully.");
        }

        internal static bool IsSelectablePreferenceOffering(
            TrackBranchOffering offering)
        {
            ArgumentNullException.ThrowIfNull(offering);

            return offering.Cycle.Status == CycleStatus.Active &&
                offering.Track.IsActive &&
                offering.Branch.IsActive &&
                OfferingService.IsAllowedByOfficialCatalog(
                    offering.Track,
                    offering.Branch,
                    offering.Cycle.Program.Code);
        }

        public async Task<Result<ApplicationDetailsDto>> SubmitApplicationAsync(Guid applicationId,
            string applicantUserId, CancellationToken cancellationToken = default)
        {
            // STEP 1: Validation
            // 1. Fetch the application along with required related data (Cycle and Preferences)
            var application = await _applicationRepository.GetFirstOrDefaultAsync(
                predicate: app => app.Id == applicationId && app.ApplicantUserId == applicantUserId,
                include: query => query
                    .Include(a => a.Cycle)
                        .ThenInclude(cycle => cycle.Program)
                    .Include(a => a.Preferences)
                        .ThenInclude(p => p.TrackBranchOffering)
                            .ThenInclude(offering => offering.Track)
                                .ThenInclude(track => track.Program)
                    .Include(a => a.Preferences)
                        .ThenInclude(p => p.TrackBranchOffering)
                            .ThenInclude(offering => offering.Branch)
                    .Include(a => a.Preferences)
                        .ThenInclude(p => p.TrackBranchOffering)
                            .ThenInclude(offering => offering.Cycle)
                                .ThenInclude(cycle => cycle.Program)
            );
            if(application == null)
            {
                return Result<ApplicationDetailsDto>.Failure(404, "Application not found or you are not authorized to submit it.");
            }

            // 2. Perform business rule validations
            var validationError = ValidateApplicationForSubmission(application);
            if (!string.IsNullOrEmpty(validationError))
            {
                return Result<ApplicationDetailsDto>.Failure(400, validationError);
            }

            // STEP 2: Eligibility Evaluation
            // 2.1 Fetch the ApplicantProfile using the string userId to get the applicantProfile Guid Id
            var applicantProfile = await _profileRepository.GetFirstOrDefaultAsync(
                predicate: p => p.UserId == applicantUserId
            );
            if(applicantProfile == null)
            {
                return Result<ApplicationDetailsDto>.Failure(400, "Applicant profile is incomplete or missing. Cannot evaluate eligibility.");
            }
            // 2.2 Evaluate eligibility
            var eligibilityResult = await _eligibilityService.EvaluateApplicantAsync(
                applicantProfile.Id,
                application.CycleId,
                application.Id
            );

            // STEP 3: Atomic State Mutation
            // CRITICAL FIX: The eligibility service likely assigned a Guid.NewGuid() to the result.
            // When attached to the EF Core graph, a non-empty Guid makes EF Core assume it is an existing entity (UPDATE).
            // 3.1 We MUST reset the Id to Guid.Empty so EF Core treats it as a completely new entity and generates an INSERT.
            eligibilityResult.Id = Guid.Empty;

            // 3.2 Explicitly link the new result to the application ID
            eligibilityResult.ApplicationId = application.Id;
            application.EligibilityResult = eligibilityResult;

            // 3.4 Update the status based on the evaluation result
            if (eligibilityResult.Passed)
            {
                application.Status = ApplicationStatus.DocumentsRequired;
            }
            else
            {
                application.Status = ApplicationStatus.EligibilityFailed;
            }

            // 3.5 Set submission and update timestamps
            application.SubmittedAt = DateTimeOffset.UtcNow;
            application.UpdatedAt = DateTimeOffset.UtcNow;

            await _applicationRepository.SaveChangesAsync();

            var applicationDetailsDto = new ApplicationDetailsDto(
                application.Id,
                application.ApplicantUserId,
                application.CycleId,
                application.Cycle?.Label ?? string.Empty,
                application.Cycle?.DeadlineUtc ?? DateTimeOffset.MinValue,
                application.Status.ToString(),
                application.CreatedAt,
                application.UpdatedAt,
                application.Preferences?.Select(p => new PreferenceDto
                (
                    p.TrackBranchOffering?.TrackId ?? Guid.Empty,
                    p.TrackBranchOffering?.BranchId ?? Guid.Empty,
                    p.Rank
                )).OrderBy(p => p.Rank).ToList() ?? new List<PreferenceDto>()
            );

            return Result<ApplicationDetailsDto>.Success(applicationDetailsDto, message: "Application submitted and evaluated successfully.");
        }


        // ============================ Helper Methods ============================
        /// <summary>
        /// Private helper method to handle all submission business rules.
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        private string? ValidateApplicationForSubmission(Domain.Entities.Application application)
        {
            // Rule A: Application must be in Draft status
            if(application.Status != ApplicationStatus.Draft)
            {
                return "Application cannot be submitted because it is no longer in Draft status.";
            }

            // Rule B: Application must have at least one selected preference
            if (application.Preferences == null || !application.Preferences.Any())
            {
                return "Cannot submit an application without selecting any preferences.";
            }

            // Rule C: Ensure Cycle is loaded properly
            if (application.Cycle == null)
            {
                return "System error: The admission cycle associated with this application could not be loaded.";
            }

            // Rule D: The cycle must be active
            if (application.Cycle.Status != CycleStatus.Active) // Assuming CycleStatus has an 'Active' enum
            {
                return $"The admission cycle '{application.Cycle.Label}' is not currently active.";
            }

            // Rule E: Deadline enforcement (Strict check against UTC time)
            if (DateTimeOffset.UtcNow > application.Cycle.DeadlineUtc)
            {
                return $"The deadline for '{application.Cycle.Label}' has passed. Submissions are no longer accepted.";
            }

            // Rule F: Stored draft preferences may predate the current catalog.
            // Revalidate them at submission so stale or crafted offerings cannot
            // bypass the current active/canonical selection policy.
            if (!HasOnlySelectableStoredPreferences(application))
            {
                return "One or more selected Track and Branch combinations are no longer available for this admission cycle.";
            }

            // Return null if all validations pass successfully
            return null;
        }

        internal static bool HasOnlySelectableStoredPreferences(
            Domain.Entities.Application application)
        {
            ArgumentNullException.ThrowIfNull(application);

            return application.Preferences is { Count: > 0 } &&
                application.Preferences.All(preference =>
                    preference.TrackBranchOffering is not null &&
                    preference.TrackBranchOffering.CycleId ==
                        application.CycleId &&
                    IsSelectablePreferenceOffering(
                        preference.TrackBranchOffering));
        }
    }
}
