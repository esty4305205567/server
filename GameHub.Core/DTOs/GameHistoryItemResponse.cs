using GameHub.Core.Enums;

namespace GameHub.Core.DTOs;

public class GameHistoryItemResponse
{
    public int SessionId { get; set; }
    public GameType GameType { get; set; }
    public DateTime? FinishedAtUtc { get; set; }
    public string HostUsername { get; set; } = string.Empty;
    public int HostScore { get; set; }
    public string? GuestUsername { get; set; }
    public int? GuestScore { get; set; }

    // null בתיקו
    public string? WinnerUsername { get; set; }
}