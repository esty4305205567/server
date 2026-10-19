using System.Security.Claims;
using GameHub.API.Infrastructure;
using GameHub.API.Models;
using GameHub.Core.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameHub.API.Controllers;

[ApiController]
[Authorize]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentPlayerId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(value, out var id)
                ? id
                : throw new InvalidOperationException(
                    "The authenticated user has no valid player id claim.");
        }
    }

    protected IActionResult ToActionResult<T>(
        Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value!);
        }

        var statusCode = result.Status switch
        {
            ResultStatus.BadRequest => StatusCodes.Status400BadRequest,
            ResultStatus.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultStatus.Forbidden => StatusCodes.Status403Forbidden,
            ResultStatus.NotFound => StatusCodes.Status404NotFound,
            ResultStatus.Conflict => StatusCodes.Status409Conflict,
            _ => throw new InvalidOperationException(
                $"Unmapped failure status: {result.Status}")
        };

        var error = new ErrorResponse
        {
            Status = statusCode,
            Message = result.Message ?? string.Empty,
            CorrelationId = CorrelationIdAccessor.Get(HttpContext)
        };

        return StatusCode(statusCode, error);
    }
}