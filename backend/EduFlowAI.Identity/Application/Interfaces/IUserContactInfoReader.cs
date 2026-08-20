using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Identity.Application.Interfaces
{
    public interface IUserContactInfoReader
    {
        Task<UserContactInfo?> GetContactInfoAsync(string userId, CancellationToken cancellationToken);
    }

    public sealed record UserContactInfo(string UserId, string Email, bool IsActive);
}
