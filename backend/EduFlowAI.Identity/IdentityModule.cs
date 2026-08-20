using System.Security.Claims;
using System.Text;
using EduFlowAI.Identity.Application.Interfaces;
using EduFlowAI.Identity.Application.Services;
using EduFlowAI.Identity.Domain.Entities;
using EduFlowAI.Identity.Infrastructure;
using EduFlowAI.Identity.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace EduFlowAI.Identity;

public static class IdentityModule
{
    public static IdentityBuilder AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

        services.AddOptions<JwtOptions>()
            .Bind(jwtSection)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Issuer),
                "Jwt:Issuer is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Audience),
                "Jwt:Audience is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.SigningKey) &&
                    Encoding.UTF8.GetByteCount(options.SigningKey) >= 32,
                "Jwt:SigningKey must contain at least 32 bytes.")
            .Validate(
                options => options.AccessTokenMinutes > 0,
                "Jwt:AccessTokenMinutes must be positive.")
            .ValidateOnStart();

        services.AddOptions<BootstrapSuperAdminOptions>()
            .Bind(configuration.GetSection(
                BootstrapSuperAdminOptions.SectionName))
            .Validate(
                options => !options.Enabled ||
                    !string.IsNullOrWhiteSpace(options.Email),
                "BootstrapSuperAdmin:Email is required when " +
                "bootstrapping is enabled.")
            .Validate(
                options => !options.Enabled ||
                    (
                        !string.IsNullOrWhiteSpace(options.Password) &&
                        options.Password.Length >= 8 &&
                        options.Password.Any(char.IsUpper) &&
                        options.Password.Any(char.IsLower) &&
                        options.Password.Any(char.IsDigit)
                    ),
                "BootstrapSuperAdmin:Password must contain at least " +
                "eight characters, an uppercase letter, a lowercase " +
                "letter and a digit.")
            .ValidateOnStart();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = CreateSigningKey(
                            jwtOptions.SigningKey),
                        ClockSkew = TimeSpan.FromMinutes(1),
                        NameClaimType = ClaimTypes.NameIdentifier,
                        RoleClaimType = ClaimTypes.Role
                    };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ValidateActiveUserAsync,
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode =
                            StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(
                            new ProblemDetails
                            {
                                Status = StatusCodes.Status401Unauthorized,
                                Title = "Authentication required",
                                Detail = "A valid access token is required."
                            });
                    },
                    OnForbidden = context =>
                    {
                        context.Response.StatusCode =
                            StatusCodes.Status403Forbidden;
                        return context.Response.WriteAsJsonAsync(
                            new ProblemDetails
                            {
                                Status = StatusCodes.Status403Forbidden,
                                Title = "Forbidden",
                                Detail = "The account does not have permission " +
                                    "to perform this action."
                            });
                    },
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Query["access_token"].FirstOrDefault();

                        if (!string.IsNullOrWhiteSpace(token) &&
                            context.HttpContext.Request.Path
                                .StartsWithSegments("/hubs/notifications"))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IOperationsManagerService,
            OperationsManagerService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IIdentityEmailSender,
            DevelopmentIdentityEmailSender>();
        services.AddScoped<IApplicantProfileLocker,
            ApplicantProfileLocker>();
        services.AddHostedService<IdentityRoleSeeder>();
        services.AddHostedService<IdentitySuperAdminSeeder>();
        services.AddScoped<IUserContactInfoReader, UserContactInfoReader>();

        return services.AddIdentityCore<AppUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = true;

            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;

            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan =
                TimeSpan.FromMinutes(15);
        })
        .AddRoles<IdentityRole>()
        .AddSignInManager()
        .AddDefaultTokenProviders();
    }

    private static SymmetricSecurityKey CreateSigningKey(
        string signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey) ||
            Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            // Options validation reports the configuration problem before
            // a token can be created. This fallback only keeps DI setup safe.
            signingKey = new string('0', 32);
        }

        return new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(signingKey));
    }

    private static async Task ValidateActiveUserAsync(
        TokenValidatedContext context)
    {
        var subject = context.Principal?.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(subject))
        {
            context.Fail(
                "The access token has no valid user identifier.");
            return;
        }

        var userManager = context.HttpContext.RequestServices
            .GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByIdAsync(subject);

        if (user is null || !user.IsActive)
        {
            context.Fail("The account is inactive.");
        }
    }
}
