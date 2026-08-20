using EduFlowAI.Identity.Application.DTOs;
using EduFlowAI.Identity.Application.Interfaces;
using EduFlowAI.Identity.Domain.Constants;
using EduFlowAI.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;

namespace EduFlowAI.Identity.Application.Services;

internal sealed class AuthService(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IJwtTokenService jwtTokenService,
    IIdentityEmailSender emailSender,
    IHostEnvironment environment) : IAuthService
{
    public async Task<IdentityOperationResult<RegisterResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var now = DateTimeOffset.UtcNow;
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            Email = email,
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
                .ValidationFailure<RegisterResponse>(createResult);
        }

        var roleResult = await userManager.AddToRoleAsync(
            user,
            AppRoles.Applicant);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return IdentityResultFactory
                .ValidationFailure<RegisterResponse>(roleResult);
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await emailSender.SendConfirmationTokenAsync(
            user,
            token,
            cancellationToken);

        return IdentityOperationResult<RegisterResponse>.Success(
            new RegisterResponse(
                user.Id,
                email,
                RequiresEmailConfirmation: true,
                DevelopmentConfirmationToken:token));
    }

    public async Task<IdentityOperationResult<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return InvalidCredentials();
        }

        if (!user.IsActive)
        {
            return IdentityOperationResult<LoginResponse>.Fail(
                new IdentityFailure(
                    IdentityFailureKind.Forbidden,
                    "Account inactive",
                    "This account has been deactivated."));
        }

        var signInResult = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (signInResult.IsLockedOut)
        {
            return IdentityOperationResult<LoginResponse>.Fail(
                new IdentityFailure(
                    IdentityFailureKind.Locked,
                    "Account locked",
                    "Too many failed attempts. Try again later."));
        }

        if (signInResult.IsNotAllowed)
        {
            return IdentityOperationResult<LoginResponse>.Fail(
                new IdentityFailure(
                    IdentityFailureKind.Forbidden,
                    "Email confirmation required",
                    "Confirm the account email before signing in."));
        }

        if (!signInResult.Succeeded)
        {
            return InvalidCredentials();
        }

        var token = await jwtTokenService.CreateAsync(user, cancellationToken);
        return IdentityOperationResult<LoginResponse>.Success(
            new LoginResponse(
                token.AccessToken,
                token.TokenType,
                token.ExpiresAtUtc,
                token.Roles));
    }

    public async Task<IdentityOperationResult<bool>> ConfirmEmailAsync(
        ConfirmEmailRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return InvalidToken<bool>(
                nameof(request.Token),
                "The email confirmation request is invalid.");
        }

        if (user.EmailConfirmed)
        {
            return IdentityOperationResult<bool>.Success(true);
        }

        var result = await userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            return IdentityResultFactory.ValidationFailure<bool>(result);
        }

        user.UpdatedAt = DateTimeOffset.UtcNow;
        var updateResult = await userManager.UpdateAsync(user);
        return updateResult.Succeeded
            ? IdentityOperationResult<bool>.Success(true)
            : IdentityResultFactory.ValidationFailure<bool>(updateResult);
    }

    public async Task<IdentityOperationResult<ForgotPasswordResponse>>
        ForgotPasswordAsync(
            ForgotPasswordRequest request,
            CancellationToken cancellationToken = default)
    {
        const string message =
            "If an eligible account exists, password reset instructions " +
            "have been issued.";

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        string? developmentToken = null;

        if (user is { IsActive: true, EmailConfirmed: true })
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            await emailSender.SendPasswordResetTokenAsync(
                user,
                token,
                cancellationToken);

            if (environment.IsDevelopment())
            {
                developmentToken = token;
            }
        }

        return IdentityOperationResult<ForgotPasswordResponse>.Success(
            new ForgotPasswordResponse(message, developmentToken));
    }

    public async Task<IdentityOperationResult<bool>> ResetPasswordAsync(
        ResetPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive)
        {
            return InvalidToken<bool>(
                nameof(request.Token),
                "The password reset request is invalid.");
        }

        var result = await userManager.ResetPasswordAsync(
            user,
            request.Token,
            request.NewPassword);
        if (!result.Succeeded)
        {
            return IdentityResultFactory.ValidationFailure<bool>(result);
        }

        user.UpdatedAt = DateTimeOffset.UtcNow;
        var updateResult = await userManager.UpdateAsync(user);
        return updateResult.Succeeded
            ? IdentityOperationResult<bool>.Success(true)
            : IdentityResultFactory.ValidationFailure<bool>(updateResult);
    }

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    private static IdentityOperationResult<LoginResponse>
        InvalidCredentials() =>
        IdentityOperationResult<LoginResponse>.Fail(new IdentityFailure(
            IdentityFailureKind.Unauthorized,
            "Invalid credentials",
            "The email or password is incorrect."));

    private static IdentityOperationResult<T> InvalidToken<T>(
        string key,
        string message) =>
        IdentityResultFactory.ValidationFailure<T>(
            key,
            message,
            "Identity validation failed");
}
