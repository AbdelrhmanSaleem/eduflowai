using EduFlowAI.Identity.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EduFlowAI.Identity.Presentation.Controllers;

public abstract class IdentityControllerBase : ControllerBase
{
    protected ActionResult ToErrorResult(IdentityFailure failure)
    {
        var statusCode = failure.Kind switch
        {
            IdentityFailureKind.Validation => StatusCodes.Status400BadRequest,
            IdentityFailureKind.Unauthorized => StatusCodes.Status401Unauthorized,
            IdentityFailureKind.Forbidden => StatusCodes.Status403Forbidden,
            IdentityFailureKind.Locked => StatusCodes.Status423Locked,
            IdentityFailureKind.Conflict => StatusCodes.Status409Conflict,
            IdentityFailureKind.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };

        if (failure.Errors is not null)
        {
            return new ObjectResult(new ValidationProblemDetails(
                failure.Errors.ToDictionary(
                    item => item.Key,
                    item => item.Value))
            {
                Status = statusCode,
                Title = failure.Title,
                Detail = failure.Detail
            })
            {
                StatusCode = statusCode
            };
        }

        return new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = failure.Title,
            Detail = failure.Detail
        })
        {
            StatusCode = statusCode
        };
    }
}
