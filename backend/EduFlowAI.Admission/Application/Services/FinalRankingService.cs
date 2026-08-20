using EduFlowAI.Admission.Application.Interfaces.Repositories;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;

namespace EduFlowAI.Admission.Application.Services
{
    public class FinalRankingService : IFinalRankingService
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IGenericRepository<SimulatedStageResult> _stageRepository;

        public FinalRankingService(IApplicationRepository applicationRepository,
            IGenericRepository<SimulatedStageResult> stageRepository)
        {
            _applicationRepository = applicationRepository;
            _stageRepository = stageRepository;
        }

        public async Task<(bool IsSuccess, string? ErrorMessage, int AdmittedCount, int WaitlistedCount)> ExecuteRankingAsync(int admittedCapacity, int waitlistCapacity)
        {
            // 1. Fetch all applications that are ready for ranking
            var eligibleApplications = await _applicationRepository.GetAllAsync(app => app.Status == ApplicationStatus.AssessmentInProgress);

            if(eligibleApplications == null || !eligibleApplications.Any())
            {
                return (false, "No applications found in the assessment phase to rank.", 0, 0);
            }

            // 2. Extract Application IDs to fetch their scores
            var applicationIds = eligibleApplications.Select(a => a.Id).ToList();

            // 3. Fetch all stage results for these applications
            var allStages = await _stageRepository.GetAllAsync(s => applicationIds.Contains(s.ApplicationId));

            // 4. Calculate total score for each application
            var applicationScores = eligibleApplications.Select(app => new
            {
                Application = app,
                // Sum the scores of all stages for this specific application (treating null as 0)
                TotalScore = allStages?.Where(s => s.ApplicationId == app.Id).Sum(s => s.Score ?? 0) ?? 0
            })
            .OrderByDescending(x => x.TotalScore)
                .ToList();  // 5. Sort descending by TotalScore

            int admittedCount = 0;
            int waitlistedCount = 0;
             // 6. Apply Ranking Logic
             for(int i = 0; i < applicationScores.Count; i++)
             {
                var item = applicationScores[i];
                if(i < admittedCapacity)
                {
                    item.Application.Status = ApplicationStatus.Admitted;
                    admittedCount++;
                }
                else if(i < (admittedCapacity + waitlistCapacity))
                {
                    item.Application.Status = ApplicationStatus.Waitlisted;
                    waitlistedCount++;
                }
                else
                {
                    item.Application.Status = ApplicationStatus.NotSelected;
                }

                item.Application.UpdatedAt = DateTimeOffset.UtcNow;
                _applicationRepository.Update(item.Application);
             }

            // 7. Save all changes to the database in a single transaction implicitly
            await _applicationRepository.SaveChangesAsync();

            return (true, null, admittedCount, waitlistedCount);
        }
    }
}
