using EduFlowAI.Communication.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Communication.Application.DbContextAbstraction
{
    public interface ICommunicationDbContext
    {
        DbSet<Notification> Notifications { get; }

        Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default);
    }
}
