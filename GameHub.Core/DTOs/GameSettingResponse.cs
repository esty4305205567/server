using GameHub.Core.Enums;

namespace GameHub.Core.DTOs;

public class GameSettingResponse
{
    public GameType GameType { get; set; }
    public int DurationSeconds { get; set; }
    public int Speed { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
