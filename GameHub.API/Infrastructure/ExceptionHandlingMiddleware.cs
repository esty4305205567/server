using GameHub.API.Models;

namespace GameHub.API.Infrastructure;

public class ExceptionHandlingMiddleware
{
    private const string UnexpectedErrorMessage = "אירעה שגיאה בלתי צפויה, נסו שוב מאוחר יותר";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            using (_logger.BeginCorrelationScope(CorrelationIdAccessor.Get(context)))
            {
                _logger.LogInformation("Request was cancelled by the client");
            }
        }
        catch (Exception exception)
        {
            var correlationId = CorrelationIdAccessor.Get(context);

            using (_logger.BeginCorrelationScope(correlationId))
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception for {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path.Value);
            }

            await WriteErrorAsync(context, correlationId);
        }
    }

    private static Task WriteErrorAsync(HttpContext context, string? correlationId)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return context.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Status = StatusCodes.Status500InternalServerError,
            Message = UnexpectedErrorMessage,
            CorrelationId = correlationId
        });
    }
}