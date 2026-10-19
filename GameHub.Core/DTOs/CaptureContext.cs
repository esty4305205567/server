using GameHub.Core.Entities;

namespace GameHub.Core.DTOs;

public class CaptureContext
{
    public required GameSession Session { get; init; }
    public required GameItem Item { get; init; }
    public required DateTime NowUtc { get; init; }
    public string? Answer { get; init; }

    // מרוץ מספרים וחידון בלבד. null באיסוף שלל
    public int? CurrentRoundOrder { get; init; }
}