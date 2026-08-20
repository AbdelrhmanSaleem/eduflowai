using EduFlowAI.Admission.Application.Interfaces;
using EduFlowAI.Admission.Application.Interfaces.Repositories;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduFlowAI.Admission.Application.Services
{
    public class AdmissionEligibilityService : IAdmissionEligibilityService
    {
        private readonly IEligibilityEngine _eligibilityEngine;
        private readonly IGenericRepository<ApplicantProfile> _applicantRepository;
        private readonly IGenericRepository<AdmissionCycle> _cycleRepository;

        public AdmissionEligibilityService(IEligibilityEngine eligibilityEngine,
            IGenericRepository<ApplicantProfile> applicantRepository,
            IGenericRepository<AdmissionCycle> cycleRepository)
        {
            _eligibilityEngine = eligibilityEngine;
            _applicantRepository = applicantRepository;
            _cycleRepository = cycleRepository;
        }

        public async Task<EligibilityResult> EvaluateApplicantAsync(Guid applicantId, Guid cycleId, Guid applicationId)
        {
            // 1. Fetch the applicant profile
            var applicantProfile = await _applicantRepository.GetFirstOrDefaultAsync(p => p.Id == applicantId);
            if(applicantProfile == null)
            {
                throw new Exception("Applicant profile not found.");
            }

            // 2. Fetch the Cycle
            var cycle = await _cycleRepository.GetFirstOrDefaultAsync(
                predicate: c => c.Id == cycleId,
                include: query => query.Include(c => c.EligibilityRule)
                .Include(c => c.Program)
                .ThenInclude(p => p.DocumentRequirements)
            );

            if (cycle == null)
            {
                throw new Exception("Admission cycle not found.");
            }

            // 3. Extract the rules throw new Exception("Admission cycle not found.")
            var cycleRule = cycle.EligibilityRule;
            if (cycleRule == null)
            {
                // Auto-eligible if no rules are configured
                return new EligibilityResult { Passed = true };
            }

            // 4. Evaluate rules using the Engine
            var engineResult = _eligibilityEngine.EvaluateApplication(applicationId, applicantProfile, cycleRule);

            return engineResult;
        }
    }
}
