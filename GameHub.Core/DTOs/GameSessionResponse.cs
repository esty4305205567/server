using GameHub.Core.Enums;

namespace GameHub.Core.DTOs;

public class GameSessionResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public GameType Type { get; set; }
    public SessionStatus Status { get; set; }
    public int DurationSeconds { get; set; }
    public int Speed { get; set; }
    public string HostUsername { get; set; } = string.Empty;
}