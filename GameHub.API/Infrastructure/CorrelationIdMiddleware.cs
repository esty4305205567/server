namespace GameHub.API.Infrastructure;

public class CorrelationIdMiddleware
{
    private const int MaxIncomingLength = 64;

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items[CorrelationIdAccessor.ItemKey] = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdAccessor.HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginCorrelationScope(correlationId))
        {
            await _next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var incoming = context.Request.Headers[CorrelationIdAccessor.HeaderName].FirstOrDefault();

        return IsAcceptable(incoming) ? incoming! : Guid.NewGuid().ToString("N");
    }

    private static bool IsAcceptable(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Length <= MaxIncomingLength
            && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
    }
}