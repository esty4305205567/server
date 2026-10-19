namespace GameHub.API.Infrastructure;

public static class CorrelationIdAccessor
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    public static string? Get(HttpContext context)
    {
        return context.Items.TryGetValue(ItemKey, out var value)
            ? value as string
            : null;
    }
}