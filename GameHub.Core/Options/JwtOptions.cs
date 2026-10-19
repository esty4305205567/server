namespace GameHub.Core.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinKeyLength = 32;

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiresMinutes { get; set; } = 60;
}