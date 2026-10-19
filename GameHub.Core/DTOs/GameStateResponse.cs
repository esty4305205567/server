using GameHub.Core.Enums;

namespace GameHub.Core.DTOs;

public class GameStateResponse
{
    public string Code { get; set; } = string.Empty;

    public SessionStatus Status { get; set; }
    public GameType Type { get; set; }

    public DateTime ServerNowUtc { get; set; }
    public int? RemainingSeconds { get; set; }

    public PlayerStateResponse Me { get; set; } = new();
    public PlayerStateResponse? Opponent { get; set; }

    public IReadOnlyList<LootBoxResponse> LootBoxes { get; set; } = Array.Empty<LootBoxResponse>();
    public RoundResponse? CurrentRound { get; set; }

    public string? Winner { get; set; }
    public bool IsDraw { get; set; }
}