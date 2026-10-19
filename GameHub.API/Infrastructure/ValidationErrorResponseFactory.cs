using GameHub.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GameHub.API.Infrastructure;

public static class ValidationErrorResponseFactory
{
    private const string FallbackMessage = "הבקשה אינה תקינה";
    private const string MalformedBodyMessage = "גוף הבקשה אינו תקין";

    public static IActionResult Create(ActionContext context)
    {
        var entries = context.ModelState
            .Where(pair => pair.Value is { Errors.Count: > 0 })
            .ToList();

        var message = HasMalformedBody(entries)
            ? MalformedBodyMessage
            : string.Join("; ", CollectMessages(entries));

        var response = new ErrorResponse
        {
            Status = StatusCodes.Status400BadRequest,
            Message = message,
            CorrelationId = CorrelationIdAccessor.Get(context.HttpContext)
        };

        return new BadRequestObjectResult(response);
    }

    private static bool HasMalformedBody(
        IEnumerable<KeyValuePair<string, ModelStateEntry?>> entries)
    {
        return entries.Any(pair =>
            pair.Key.StartsWith('$') ||
            pair.Value!.Errors.Any(error => error.Exception is not null));
    }

    private static IEnumerable<string> CollectMessages(
        IEnumerable<KeyValuePair<string, ModelStateEntry?>> entries)
    {
        return entries
            .SelectMany(pair => pair.Value!.Errors)
            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                ? FallbackMessage
                : error.ErrorMessage)
            .Distinct();
    }
}