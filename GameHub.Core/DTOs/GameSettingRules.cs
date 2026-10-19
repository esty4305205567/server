namespace GameHub.Core.Validation;

public static class GameSettingRules
{
    public const int MinDurationSeconds = 30;
    public const int MaxDurationSeconds = 600;

    public const int MinSpeed = 1;
    public const int MaxSpeed = 5;

    public const string DurationOutOfRangeMessage = "משך המשחק חייב להיות בין 30 ל-600 שניות";
    public const string SpeedOutOfRangeMessage = "המהירות חייבת להיות בין 1 ל-5";
}