using EduFlowAI.Shared.Kernel.Common;
using Microsoft.AspNetCore.Mvc;

namespace EduFlowAI.Admission.Presentation.Controllers;

internal static class AdmissionControllerResults
{
    public static ActionResult<Result<T>> ToActionResult<T>(
        this ControllerBase controller,
        Result<T> result)
    {
        return controller.StatusCode(result.StatusCode, result);
    }
}
