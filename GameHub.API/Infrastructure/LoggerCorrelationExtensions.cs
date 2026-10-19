namespace GameHub.API.Infrastructure;

public static class LoggerCorrelationExtensions
{
    // Must match ${scopeproperty:CorrelationId} in nlog.config.
    public const string ScopeKey = "CorrelationId";

    public static IDisposable? BeginCorrelationScope(this ILogger logger, string? correlationId)
    {
        if (string.IsNullOrEmpty(correlationId))
        {
            return null;
        }

        return logger.BeginScope(new Dictionary<string, object> { [ScopeKey] = correlationId });
    }
}