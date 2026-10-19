using GameHub.Core.Interfaces;

namespace GameHub.Core.Entities;

public class GameItem : IVersioned
{
    public int Id { get; set; }

    public int SessionId { get; set; }
    public GameSession Session { get; set; } = default!;

    public int Order { get; set; }
    public string DisplayText { get; set; } = string.Empty;
    public int Points { get; set; }
    public int? Slot { get; set; }
    // איסוף שלל בלבד
    public double? X { get; set; }
    public double? Y { get; set; }
    public int? AppearAtMs { get; set; }
    public int? DisappearAtMs { get; set; }

    // מרוץ מספרים: 5 מספרים מופרדים בפסיק
    public string? Options { get; set; }

    // מרוץ מספרים וחידון. לא יוצא באף DTO
    public string? CorrectAnswer { get; set; }

    public int? CapturedByPlayerId { get; set; }
    public DateTime? CapturedAtUtc { get; set; }

    // ה-concurrency token של התחרות על הפריט
    public Guid Version { get; set; } = Guid.NewGuid();
}