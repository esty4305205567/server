namespace GameHub.Core.Validation;

public static class UsernameRules
{
    public const int MinLength = 3;
    public const int MaxLength = 50;

    public const string Pattern = @"^[A-Za-z0-9]{3,50}\z";

    public const string InvalidMessage =
        "שם המשתמש חייב להכיל 3 עד 50 תווים: אותיות באנגלית וספרות בלבד";
}