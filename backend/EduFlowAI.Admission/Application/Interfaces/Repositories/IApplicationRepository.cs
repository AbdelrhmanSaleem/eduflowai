using System;
using System.Threading.Tasks;

namespace EduFlowAI.Admission.Application.Interfaces.Repositories
{
    public interface IApplicationRepository : IGenericRepository<Domain.Entities.Application>
    {
        /// <summary>
        /// Checks if a user has an existing application for a given admission cycle.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="cycleId"></param>
        /// <returns></returns>
        Task<bool> HasExistingApplicationAsync(string userId, Guid cycleId);
    }
}
