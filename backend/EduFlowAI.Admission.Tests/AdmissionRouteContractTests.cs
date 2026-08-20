using EduFlowAI.Admission.Presentation.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Reflection;

namespace EduFlowAI.Admission.Tests;

public sealed class AdmissionRouteContractTests
{
    public static TheoryData<Type, string> ControllerRoutes =>
        new()
        {
            { typeof(TracksController), "api/tracks" },
            { typeof(AdminInstitutionsController), "api/admin/institutions" },
            { typeof(AdminProgramsController), "api/admin/programs" },
            { typeof(AdminTracksController), "api/admin/tracks" },
            { typeof(AdminBranchesController), "api/admin/branches" },
            { typeof(AdminCyclesController), "api/admin/cycles" },
            { typeof(AdminAdmissionDashboardController), "api/admin/dashboard" }
        };

    public static TheoryData<Type> AdminControllers =>
        new()
        {
            typeof(AdminInstitutionsController),
            typeof(AdminProgramsController),
            typeof(AdminTracksController),
            typeof(AdminBranchesController),
            typeof(AdminCyclesController),
            typeof(AdminAdmissionDashboardController)
        };

    public static TheoryData<Type, string, Type, string?, bool> ActionRoutes =>
        new()
        {
            { typeof(TracksController), nameof(TracksController.GetTracks), typeof(HttpGetAttribute), null, true },
            { typeof(TracksController), nameof(TracksController.GetTrack), typeof(HttpGetAttribute), "{trackId:guid}", true },

            { typeof(AdminInstitutionsController), nameof(AdminInstitutionsController.GetInstitutions), typeof(HttpGetAttribute), null, false },
            { typeof(AdminInstitutionsController), nameof(AdminInstitutionsController.CreateInstitution), typeof(HttpPostAttribute), null, false },
            { typeof(AdminInstitutionsController), nameof(AdminInstitutionsController.UpdateInstitution), typeof(HttpPutAttribute), "{institutionId:guid}", false },

            { typeof(AdminProgramsController), nameof(AdminProgramsController.GetPrograms), typeof(HttpGetAttribute), null, false },
            { typeof(AdminProgramsController), nameof(AdminProgramsController.CreateProgram), typeof(HttpPostAttribute), null, false },
            { typeof(AdminProgramsController), nameof(AdminProgramsController.UpdateProgram), typeof(HttpPutAttribute), "{programId:guid}", false },
            { typeof(AdminProgramsController), nameof(AdminProgramsController.DeleteProgram), typeof(HttpDeleteAttribute), "{programId:guid}", false },
            { typeof(AdminProgramsController), nameof(AdminProgramsController.GetProgramDocumentRequirements), typeof(HttpGetAttribute), "{programId:guid}/document-requirements", false },
            { typeof(AdminProgramsController), nameof(AdminProgramsController.UpdateProgramDocumentRequirements), typeof(HttpPutAttribute), "{programId:guid}/document-requirements", false },

            { typeof(AdminTracksController), nameof(AdminTracksController.GetTracks), typeof(HttpGetAttribute), null, false },
            { typeof(AdminTracksController), nameof(AdminTracksController.CreateTrack), typeof(HttpPostAttribute), null, false },
            { typeof(AdminTracksController), nameof(AdminTracksController.UpdateTrack), typeof(HttpPutAttribute), "{trackId:guid}", false },

            { typeof(AdminBranchesController), nameof(AdminBranchesController.GetBranches), typeof(HttpGetAttribute), null, false },
            { typeof(AdminBranchesController), nameof(AdminBranchesController.CreateBranch), typeof(HttpPostAttribute), null, false },
            { typeof(AdminBranchesController), nameof(AdminBranchesController.UpdateBranch), typeof(HttpPutAttribute), "{branchId:guid}", false },

            { typeof(AdminCyclesController), nameof(AdminCyclesController.GetCycles), typeof(HttpGetAttribute), null, false },
            { typeof(AdminCyclesController), nameof(AdminCyclesController.CreateCycle), typeof(HttpPostAttribute), null, false },
            { typeof(AdminCyclesController), nameof(AdminCyclesController.UpdateEligibilityRule), typeof(HttpPutAttribute), "{cycleId:guid}/eligibility-rule", false },
            { typeof(AdminCyclesController), nameof(AdminCyclesController.CreateOffering), typeof(HttpPostAttribute), "{cycleId:guid}/offerings", false },
            { typeof(AdminCyclesController), nameof(AdminCyclesController.UpdateOffering), typeof(HttpPutAttribute), "{cycleId:guid}/offerings/{offeringId:guid}", false },
            { typeof(AdminCyclesController), nameof(AdminCyclesController.DeleteOffering), typeof(HttpDeleteAttribute), "{cycleId:guid}/offerings/{offeringId:guid}", false },
            { typeof(AdminCyclesController), nameof(AdminCyclesController.ActivateCycle), typeof(HttpPostAttribute), "{cycleId:guid}/activate", false },
            { typeof(AdminCyclesController), nameof(AdminCyclesController.CloseCycle), typeof(HttpPostAttribute), "{cycleId:guid}/close", false },

            { typeof(AdminAdmissionDashboardController), nameof(AdminAdmissionDashboardController.GetDashboard), typeof(HttpGetAttribute), null, false }
        };

    [Theory]
    [MemberData(nameof(ControllerRoutes))]
    public void Controllers_preserve_api_controller_and_base_route_contracts(
        Type controllerType,
        string expectedRoute)
    {
        Assert.NotNull(
            controllerType.GetCustomAttribute<ApiControllerAttribute>(inherit: true));

        var route = Assert.Single(
            controllerType.GetCustomAttributes<RouteAttribute>(inherit: true));

        Assert.Equal(expectedRoute, route.Template);
    }

    [Theory]
    [MemberData(nameof(AdminControllers))]
    public void Admin_controllers_require_the_super_admin_role(
        Type controllerType)
    {
        var authorize = Assert.Single(
            controllerType.GetCustomAttributes<AuthorizeAttribute>(inherit: true));

        Assert.Equal("SuperAdmin", authorize.Roles);
        Assert.False(
            controllerType.IsDefined(
                typeof(AllowAnonymousAttribute),
                inherit: true));
    }

    [Fact]
    public void Public_tracks_controller_does_not_require_class_level_authorization()
    {
        Assert.Empty(
            typeof(TracksController)
                .GetCustomAttributes<AuthorizeAttribute>(inherit: true));
    }

    [Theory]
    [MemberData(nameof(ActionRoutes))]
    public void Actions_preserve_http_method_route_and_anonymous_contracts(
        Type controllerType,
        string actionName,
        Type httpAttributeType,
        string? expectedTemplate,
        bool expectedAllowAnonymous)
    {
        var action = controllerType.GetMethod(
            actionName,
            BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(action);

        var httpAttribute = Assert.IsAssignableFrom<HttpMethodAttribute>(
            Assert.Single(
                action.GetCustomAttributes(
                    httpAttributeType,
                    inherit: true)));

        Assert.Equal(expectedTemplate, httpAttribute.Template);
        Assert.Equal(
            expectedAllowAnonymous,
            action.IsDefined(
                typeof(AllowAnonymousAttribute),
                inherit: true));
    }
}
