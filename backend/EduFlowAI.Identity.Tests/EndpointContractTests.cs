using EduFlowAI.Identity.Domain.Constants;
using EduFlowAI.Identity.Presentation.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduFlowAI.Identity.Tests;

public sealed class EndpointContractTests
{
    [Fact]
    public void AuthController_ExposesAllRequiredActions()
    {
        Assert.Equal("api/auth", RouteOf<AuthController>());
        AssertPost<AuthController>("Register", "register");
        AssertPost<AuthController>("Login", "login");
        AssertPost<AuthController>("ConfirmEmail", "confirm-email");
        AssertPost<AuthController>("ForgotPassword", "forgot-password");
        AssertPost<AuthController>("ResetPassword", "reset-password");
    }

    [Fact]
    public void ProfileController_RequiresApplicantRole()
    {
        Assert.Equal("api/profile", RouteOf<ProfileController>());
        Assert.NotNull(typeof(ProfileController).GetMethod("Get")!
            .GetCustomAttributes(typeof(HttpGetAttribute), true)
            .SingleOrDefault());
        Assert.NotNull(typeof(ProfileController).GetMethod("Put")!
            .GetCustomAttributes(typeof(HttpPutAttribute), true)
            .SingleOrDefault());
        Assert.Equal(
            AppRoles.Applicant,
            typeof(ProfileController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>()
                .Single()
                .Roles);
    }

    [Fact]
    public void OperationsManagerController_RequiresSuperAdminRole()
    {
        Assert.Equal(
            "api/admin/operations-managers",
            RouteOf<OperationsManagersController>());
        AssertPost<OperationsManagersController>("Create", null);
        var patch = typeof(OperationsManagersController)
            .GetMethod("Deactivate")!
            .GetCustomAttributes(typeof(HttpPatchAttribute), true)
            .Cast<HttpPatchAttribute>()
            .Single();
        Assert.Equal("{userId}/deactivate", patch.Template);
        Assert.Equal(
            AppRoles.SuperAdmin,
            typeof(OperationsManagersController)
                .GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>()
                .Single()
                .Roles);
    }

    private static string? RouteOf<TController>() =>
        typeof(TController)
            .GetCustomAttributes(typeof(RouteAttribute), true)
            .Cast<RouteAttribute>()
            .Single()
            .Template;

    private static void AssertPost<TController>(
        string methodName,
        string? template)
    {
        var attribute = typeof(TController)
            .GetMethod(methodName)!
            .GetCustomAttributes(typeof(HttpPostAttribute), true)
            .Cast<HttpPostAttribute>()
            .Single();
        Assert.Equal(template, attribute.Template);
    }
}
