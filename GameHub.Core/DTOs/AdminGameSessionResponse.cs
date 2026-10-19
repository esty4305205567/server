using GameHub.Core.Enums;

namespace GameHub.Core.DTOs;

public class AdminGameSessionResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public GameType Type { get; set; }
    public SessionStatus Status { get; set; }
    public string HostUsername { get; set; } = string.Empty;
    public string? GuestUsername { get; set; }
    public int DurationSeconds { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
}