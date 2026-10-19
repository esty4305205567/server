namespace GameHub.Core.Validation;

public static class PasswordRules
{
    public const int MinLength = 8;
    public const int MaxLength = 72;

    public const string TooShortMessage = "הסיסמה חייבת להכיל לפחות 8 תווים";
    public const string TooLongMessage = "הסיסמה ארוכה מדי, עד 72 תווים";
}