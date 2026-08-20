using EduFlowAI.Admission.Application.DTOs;

namespace EduFlowAI.Admission.Application.Interfaces.Services
{
    public interface IStatusTransitionCoordinator
    {
        /// <summary>
        /// // Handles the business logic for a student withdrawing their application.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="applicantUserId"></param>
        /// <returns></returns>
        Task<(bool IsSuccess, string ErrorMessage, ApplicationStatusDto? Data)> WithdrawApplicationAsync(Guid applicationId, string applicantUserId);

        /// <summary>
        /// Handles the state transition based on the document review outcome from the Document Module.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <param name="reviewResult"></param>
        /// <returns></returns>
        Task<(bool IsSuccess, string ErrorMessage, ApplicationStatusDto? Data)> ProcessDocumentReviewAsync(Guid applicationId, DocumentReviewResultDto reviewResult);
        // Note: We will add more methods here later for document verification transitions, etc.

        /// <summary>
        /// Transitions the application to UnderDocumentVerification.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        Task<(bool IsSuccess, string ErrorMessage)> MarkDocumentVerificationStartedAsync(Guid applicationId);

        /// <summary>
        /// Transitions the application to the Admitted state and automatically generates the required enrollment checklist tasks.
        /// </summary>
        /// <param name="applicationId"></param>
        /// <returns></returns>
        Task<(bool IsSuccess, string ErrorMessage, ApplicationStatusDto? Data)> AdmitApplicantAsync(Guid applicationId);
    }
}
