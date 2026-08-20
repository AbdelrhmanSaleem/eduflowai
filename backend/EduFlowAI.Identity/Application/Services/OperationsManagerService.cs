using EduFlowAI.Identity.Application.DTOs;
using EduFlowAI.Identity.Application.Interfaces;
using EduFlowAI.Identity.Domain.Constants;
using EduFlowAI.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace EduFlowAI.Identity.Application.Services;

internal sealed class OperationsManagerService(
    UserManager<AppUser> userManager) : IOperationsManagerService
{
    public async Task<IdentityOperationResult<OperationsManagerResponse>>
        CreateAsync(CreateOperationsManagerRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            PreferredLanguage = request.PreferredLanguage,
            IsActive = true,
            LockoutEnabled = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return IdentityResultFactory
                .ValidationFailure<OperationsManagerResponse>(createResult);
        }

        var roleResult = await userManager.AddToRoleAsync(
            user,
            AppRoles.OperationsManager);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return IdentityResultFactory
                .ValidationFailure<OperationsManagerResponse>(roleResult);
        }

        return IdentityOperationResult<OperationsManagerResponse>.Success(
            ToResponse(user));
    }

    public async Task<IdentityOperationResult<bool>> DeactivateAsync(
        string userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null ||
            !await userManager.IsInRoleAsync(
                user,
                AppRoles.OperationsManager))
        {
            return IdentityOperationResult<bool>.Fail(new IdentityFailure(
                IdentityFailureKind.NotFound,
                "Operations Manager not found"));
        }

        if (!user.IsActive)
        {
            return IdentityOperationResult<bool>.Success(true);
        }

        var now = DateTimeOffset.UtcNow;
        user.IsActive = false;
        user.DeactivatedAt = now;
        user.UpdatedAt = now;
        user.SecurityStamp = Guid.NewGuid().ToString();

        var result = await userManager.UpdateAsync(user);
        return result.Succeeded
            ? IdentityOperationResult<bool>.Success(true)
            : IdentityResultFactory.ValidationFailure<bool>(result);
    }

    private static OperationsManagerResponse ToResponse(AppUser user) =>
        new(
            user.Id,
            user.Email ?? string.Empty,
            user.IsActive,
            user.CreatedAt,
            user.DeactivatedAt);
}
