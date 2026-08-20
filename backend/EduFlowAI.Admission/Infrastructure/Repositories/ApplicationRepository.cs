using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.Admission.Application.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Infrastructure.Repositories
{
    public class ApplicationRepository : GenericRepository<Domain.Entities.Application>, IApplicationRepository
    {
        public ApplicationRepository(IAdmissionDbContext dbContext)
            : base(dbContext)
        {
        }

        public async Task<bool> HasExistingApplicationAsync(string userId, Guid cycleId)
        {
            // Check if there is any application matching the given user ID and cycle ID
            // We use _dbSet which is inherited from the base GenericRepository
            return await _dbSet.AnyAsync(a => a.ApplicantUserId == userId && a.CycleId == cycleId);
        }
    }
}
