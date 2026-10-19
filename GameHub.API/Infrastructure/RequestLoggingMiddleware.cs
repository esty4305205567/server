using System.Diagnostics;

namespace GameHub.API.Infrastructure;

public class RequestLoggingMiddleware
{
    private const string SwaggerPrefix = "/swagger";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(
        RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments(SwaggerPrefix))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var failed = true;

        try
        {
            await _next(context);
            failed = false;
        }
        finally
        {
            stopwatch.Stop();

            var statusCode = failed
                ? StatusCodes.Status500InternalServerError
                : context.Response.StatusCode;

            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs} ms",
                context.Request.Method,
                context.Request.Path.Value,
                statusCode,
                stopwatch.ElapsedMilliseconds);
        }
    }
}