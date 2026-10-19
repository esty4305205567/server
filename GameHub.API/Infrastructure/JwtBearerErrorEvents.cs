using GameHub.API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace GameHub.API.Infrastructure;

public static class JwtBearerErrorEvents
{
    public const string UnauthorizedMessage = "יש להתחבר כדי לבצע פעולה זו";
    public const string ForbiddenMessage = "אין לך הרשאה לבצע פעולה זו";

    public static JwtBearerEvents Create()
    {
        return new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                context.Response.Headers["WWW-Authenticate"] = context.AuthenticateFailure is null
                    ? "Bearer"
                    : "Bearer error=\"invalid_token\"";

                await WriteAsync(
                    context.HttpContext, StatusCodes.Status401Unauthorized, UnauthorizedMessage);
            },
            OnForbidden = context => WriteAsync(
                context.HttpContext, StatusCodes.Status403Forbidden, ForbiddenMessage)
        };
    }

    private static Task WriteAsync(HttpContext httpContext, int statusCode, string message)
    {
        var response = httpContext.Response;

        if (response.HasStarted)
        {
            return Task.CompletedTask;
        }

        response.StatusCode = statusCode;

        return response.WriteAsJsonAsync(new ErrorResponse
        {
            Status = statusCode,
            Message = message,
            CorrelationId = CorrelationIdAccessor.Get(httpContext)
        });
    }
}