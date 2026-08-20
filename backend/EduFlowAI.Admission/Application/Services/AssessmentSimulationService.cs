using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Admission.Application.Extensions;
using EduFlowAI.Admission.Application.Interfaces.Repositories;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EduFlowAI.Admission.Application.Services
{
    public class AssessmentSimulationService : IAssessmentSimulationService
    {
        private readonly IGenericRepository<SimulatedStageResult> _stageRepository;
        private readonly IApplicationRepository _applicationRepository;
        private readonly Random _random;

        public AssessmentSimulationService(IGenericRepository<SimulatedStageResult> stageRepository,
            IApplicationRepository applicationRepository)
        {
            _stageRepository = stageRepository;
            _applicationRepository = applicationRepository;
            _random = new Random();
        }

        public async Task<(bool isSuccess, string? ErrorMessage, SimulatedStageDto? Data)> SimulateStageAsync(Guid applicationId, SelectionStage stage, Guid? trackId = null)
        {
            // 1. Verify that the application exists
            var application = await _applicationRepository.GetFirstOrDefaultAsync(app => app.Id == applicationId);
            if(application == null)
            {
                return (false, "Application is not found", null);
            }

            // 2. Check if the stage has already been simulated for this application to avoid duplicates
            var existingStage = await _stageRepository.GetFirstOrDefaultAsync(
                s => s.ApplicationId == applicationId 
                && s.Stage == stage
                && s.TrackId == trackId
            );
            if(existingStage != null)
            {
                return (false, $"Stage '{stage}' for track '{trackId}' has already been simulated for this application.", null);
            }

            /*
             * 3. Business Logic: Simulate the score
             * For simulation purposes, we generate a random score. 
             * Let's assume MaxScore is 100 for all exams for simplicity.
             */

            decimal maxScore = 100m;
            decimal simulatedScore = _random.Next(61, 101);

            // Determine if passed (e.g., Passing score is 60)
            var stageResult = simulatedScore >= 60 ? StageResult.Passed : StageResult.NotPassed;

            // 4. Create the Domain Entity
            var newStageResult = new SimulatedStageResult
            {
                ApplicationId = applicationId,
                TrackId = trackId,
                Stage = stage,
                Score = simulatedScore,
                MaxScore = maxScore,
                Result = stageResult,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            // 5. Save to Database
            await _stageRepository.AddAsync(newStageResult);
            await _stageRepository.SaveChangesAsync();

            // 6. Map to DTO and return
            var displayInfo = newStageResult.Stage.GetDisplayInfo();

            var stageResultDto = new SimulatedStageDto
            {
                StageId = newStageResult.Id,
                StageType = newStageResult.Stage.ToString(),
                Title = displayInfo.Name,
                Description = displayInfo.Description,
                Score = newStageResult.Score,
                MaxScore = newStageResult.MaxScore,
                Result = newStageResult.Result.ToString(),
                UpdatedAt = newStageResult.UpdatedAt ?? newStageResult.CreatedAt
            };

            return (true, null, stageResultDto);
        }

        public async Task<IEnumerable<SimulatedStageDto>> GetApplicantStagesAsync(Guid applicationId)
        {
            var stages = await _stageRepository.GetAllAsync(
                predicate: s => s.ApplicationId == applicationId,
                include: query => query.Include(s => s.Track)
            );
            if(stages == null)
            {
                return Enumerable.Empty<SimulatedStageDto>();
            }

            // Map and Order by the Stage Enum value to maintain proper timeline flow
            return stages.OrderBy(s => s.Stage).Select(s =>
            {
                var displayInfo = s.Stage.GetDisplayInfo(); // Extract UI text

                // 2. Dynamically construct the Title based on whether a Track exists
                string finalTitle = s.Track != null ? $"{displayInfo.Name} - {s.Track.Name}"
                                                    : displayInfo.Name;

                return new SimulatedStageDto
                {
                    StageId = s.Id,
                    StageType = s.Stage.ToString(),
                    Title = finalTitle,              // Map Title
                    Description = displayInfo.Description, // Map Description
                    Score = s.Score,
                    MaxScore = s.MaxScore,
                    Result = s.Result.ToString(),
                    TrackName = s.Track?.Name,
                    UpdatedAt = s.UpdatedAt ?? s.CreatedAt
                };
            }).ToList();
        }

        public async Task<(bool IsSuccess, string? ErrorMessage, int ProcessedCount)> BulkSimulateStageAsync(Guid cycleId, SelectionStage stage)
        {
            // Fetch all applications in the cycle that are currently in the assessment phase
            var applications = await _applicationRepository.GetAllAsync(
                predicate: a => a.CycleId == cycleId && a.Status == ApplicationStatus.AssessmentInProgress,
                include: q => q.Include(a => a.Preferences).ThenInclude(p => p.TrackBranchOffering)
            );
            if (applications == null || !applications.Any())
            {
                return (false, "No eligible applications found for simulation in this cycle.", 0);
            }

            int processedCount = 0;

            // Determine if this stage requires track-specific simulations based on our business logic
            bool isTrackSpecific = stage == SelectionStage.ProgrammingExam || stage == SelectionStage.TechnicalInterview;

            // Iterate through the applications and reuse the individual simulation logic
            foreach (var app in applications)
            {
                if (isTrackSpecific)
                {
                    // Simulate for Each distinct track the applicant applied to
                    if (app.Preferences != null)
                    {
                        var distinctTrackIds = app.Preferences
                            .Select(p => p.TrackBranchOffering.TrackId)
                            .Distinct();

                        foreach (var trackId in distinctTrackIds)
                        {
                            var (isSuccess, _, _) = await SimulateStageAsync(app.Id, stage, trackId);
                            if (isSuccess)
                            {
                                processedCount++;
                            }
                        }
                    }
                }
                else
                {
                    // Global exams (English/IQ, SoftSkills) are simulated once with TrackId = null
                    var (isSuccess, _, _) = await SimulateStageAsync(app.Id, stage, null);
                    if (isSuccess)
                    {
                        processedCount++;
                    }
                }
            }

            return (true, null, processedCount);
        }
    }
}
