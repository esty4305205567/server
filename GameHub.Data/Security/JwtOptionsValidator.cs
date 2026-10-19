using GameHub.Core.Options;
using Microsoft.Extensions.Options;

namespace GameHub.Data.Security;

public class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        if (options.Key.Length < JwtOptions.MinKeyLength)
        {
            failures.Add(
                $"Jwt:Key חסר או קצר מ-{JwtOptions.MinKeyLength} תווים. יש לבדוק את ה-User Secrets של GameHub.API.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("Jwt:Issuer חסר. יש לבדוק את appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("Jwt:Audience חסר. יש לבדוק את appsettings.json.");
        }

        if (options.ExpiresMinutes <= 0)
        {
            failures.Add("Jwt:ExpiresMinutes חייב להיות מספר חיובי.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}