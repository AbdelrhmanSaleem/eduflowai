using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;

namespace EduFlowAI.Admission.Application.Interfaces.Services
{
    public interface ITimelineCalculator
    {
        // Calculates the timeline progress and status for all stages
        TimelineStateResult Calculate(ApplicationStatus status,
            IEnumerable<SimulatedStageResult> stages,
            EligibilityResult? eligibilityResult);
    }
}
