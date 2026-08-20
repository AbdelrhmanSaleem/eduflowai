using EduFlowAI.Admission.Application.Models;

namespace EduFlowAI.Admission.Application.Interfaces.Services
{
    public interface IAdmissionEmailNotificationService
    {
        Task SendAsync(AdmissionEmailNotification notification,
            CancellationToken cancellationToken = default);
    }
}
