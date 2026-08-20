using EduFlowAI.Identity.Application.DbContextAbstraction;
using EduFlowAI.Identity.Application.Interfaces;
using EduFlowAI.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Identity.Application.Services
{
    public class UserContactInfoReader(IIdentityDbContext dbContext) : IUserContactInfoReader
    {
        public async Task<UserContactInfo?> GetContactInfoAsync(string userId, CancellationToken cancellationToken)
        {
            var user = await dbContext.AppUsers.FindAsync(userId, cancellationToken);
            if (user == null)
            {
                return null;
            }

            var info = new UserContactInfo(
                UserId: user.Id,
                Email: user.Email!,
                IsActive: user.IsActive
            );

            return info;

        }
    }
}
