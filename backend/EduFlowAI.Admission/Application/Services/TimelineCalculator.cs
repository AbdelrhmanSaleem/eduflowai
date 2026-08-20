using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Admission.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Application.Services
{
    public class TimelineCalculator : ITimelineCalculator
    {
        public TimelineStateResult Calculate(ApplicationStatus status, IEnumerable<SimulatedStageResult> stages, EligibilityResult? eligibilityResult)
        {
            // 1. Initialize default states
            int percentage = 0;
            string app = "Pending", elig = "Pending", verif = "Pending",
                   eng = "Pending", tech = "Pending", intv = "Pending", final = "Pending";

            // 2. Application Phase
            if (status == ApplicationStatus.Draft || status == ApplicationStatus.None)
            {
                return new TimelineStateResult { Percentage = 0, AppStatus = "InProgress" };
            }
            app = "Passed";

            // 3. Eligibility Phase
            if (eligibilityResult != null)
            {
                elig = eligibilityResult.Passed ? "Passed" : "Failed";
                percentage = 16;

                if (!eligibilityResult.Passed)
                {
                    return CreateResult(percentage, app, elig, verif, eng, tech, intv, final);
                }
            }
            else if (status == ApplicationStatus.EligibilityFailed)
            {
                return CreateResult(16, app, "Failed", verif, eng, tech, intv, final);
            }
            else
            {
                return CreateResult(16, app, "InProgress", verif, eng, tech, intv, final);
            }

            // 4. Verification Phase
            if (status == ApplicationStatus.DocumentsRequired ||
                status == ApplicationStatus.UnderDocumentVerification ||
                status == ApplicationStatus.NeedsHumanReview)
            {
                return CreateResult(33, app, elig, "InProgress", eng, tech, intv, final);
            }
            else if (status == ApplicationStatus.DocumentRejected)
            {
                return CreateResult(33, app, elig, "Failed", eng, tech, intv, final);
            }
            verif = "Passed";

            // 5. Assessment Phases
            var englishStage = stages.FirstOrDefault(s => s.Stage == SelectionStage.EnglishExamAndIq);
            var technicalStage = stages.FirstOrDefault(s => s.Stage == SelectionStage.ProgrammingExam || s.Stage == SelectionStage.TechnicalInterview);
            var interviewStage = stages.FirstOrDefault(s => s.Stage == SelectionStage.SoftSkillsInterview);

            // English Stage Evaluation
            if (englishStage != null)
            {
                eng = englishStage.Result == StageResult.Passed ? "Passed" :
                     (englishStage.Result == StageResult.NotPassed ? "Failed" : "InProgress");
                percentage = 50;
            }
            else if (status == ApplicationStatus.AssessmentInProgress)
            {
                eng = "InProgress";
                percentage = 50;
            }

            // Technical Stage Evaluation
            if (eng == "Passed")
            {
                if (technicalStage != null)
                {
                    tech = technicalStage.Result == StageResult.Passed ? "Passed" :
                          (technicalStage.Result == StageResult.NotPassed ? "Failed" : "InProgress");
                    percentage = 66;
                }
                else if (englishStage?.Result == StageResult.Passed)
                {
                    tech = "InProgress";
                    percentage = 66;
                }
            }

            // Interviews Stage Evaluation
            if (tech == "Passed")
            {
                if (interviewStage != null)
                {
                    intv = interviewStage.Result == StageResult.Passed ? "Passed" :
                          (interviewStage.Result == StageResult.NotPassed ? "Failed" : "InProgress");
                    percentage = 83;
                }
                else if (technicalStage?.Result == StageResult.Passed)
                {
                    intv = "InProgress";
                    percentage = 83;
                }
            }

            // 6. Final Result Phase
            if (status == ApplicationStatus.Admitted)
            {
                final = "Passed";
                percentage = 100;
            }
            else if (status == ApplicationStatus.Waitlisted)
            {
                final = "InProgress";
                percentage = 100;
            }
            else if (status == ApplicationStatus.NotSelected)
            {
                final = "Failed";
                percentage = 100;
            }

            return CreateResult(percentage, app, elig, verif, eng, tech, intv, final);
        }

        // ====================== Helper Methods ======================
        // Helper method to keep code clean and avoid repetitive object initialization
        private TimelineStateResult CreateResult(int percentage, string app, string elig, string verif, string eng, string tech, string intv, string final)
        {
            return new TimelineStateResult
            {
                Percentage = percentage,
                AppStatus = app,
                EligStatus = elig,
                VerifStatus = verif,
                EngStatus = eng,
                TechStatus = tech,
                IntStatus = intv,
                FinalStatus = final
            };
        }
    }
}
