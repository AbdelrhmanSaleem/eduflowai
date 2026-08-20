using EduFlowAI.Admission.Domain.Enums;

namespace EduFlowAI.Admission.Application.Extensions
{
    public static class ApplicationStatusExtensions
    {
        /// <summary>
        /// Converts the ApplicationStatus enum into a user-friendly UI message.
        /// </summary>
        public static string GetDisplayMessage(this ApplicationStatus status)
        {
            return status switch
            {
                ApplicationStatus.None => "No active application found.",
                ApplicationStatus.Draft => "Your application is incomplete. Please submit it before the deadline.",
                ApplicationStatus.EligibilityFailed => "Unfortunately, you do not meet the eligibility criteria for this intake.",
                ApplicationStatus.DocumentsRequired => "Please upload the required documents to proceed.",
                ApplicationStatus.UnderDocumentVerification => "Your documents are currently being verified by our AI system.",
                ApplicationStatus.NeedsHumanReview => "Your documents require manual review. We will notify you once done.",
                ApplicationStatus.DocumentRejected => "Some of your documents were rejected. Please review and re-upload.",
                ApplicationStatus.AssessmentInProgress => "Your assessments are currently in progress. Please check your schedule.",
                ApplicationStatus.Admitted => "Congratulations! You have been admitted.",
                ApplicationStatus.Waitlisted => "You have been placed on the waitlist. We will notify you if a spot opens.",
                ApplicationStatus.NotSelected => "We regret to inform you that you were not selected for this intake.",
                ApplicationStatus.Withdrawn => "You have successfully withdrawn your application.",
                ApplicationStatus.Expired => "The deadline has passed and your application has expired.",
                _ => "You have no unread communications regarding your application at this time."
            };
        }
    }
}
