using GameHub.Core.Enums;

namespace GameHub.Core.Entities;

public class GameSetting
{
    public GameType GameType { get; set; }   // מפתח
    public int DurationSeconds { get; set; }
    public int Speed { get; set; }           // בשימוש באיסוף שלל בלבד
    public DateTime UpdatedAtUtc { get; set; }
}