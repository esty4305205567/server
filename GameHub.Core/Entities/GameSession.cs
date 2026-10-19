using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace GameHub.Core.Entities;

public class GameSession : IVersioned
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public GameType Type { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Waiting;

    public int HostId { get; set; }
    public int? GuestId { get; set; }

    // מועתקים מ-GameSetting בעת פתיחת החדר, כדי ששינוי של המנהל לא ישפיע על משחק שרץ
    public int DurationSeconds { get; set; }
    public int Speed { get; set; }

    // איסוף שלל בלבד. זמן התצוגה של כל תיבה במילישניות, נקבע בפתיחת החדר.
    // null במרוץ מספרים ובחידון
    public int? DisplayMs { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? FinishedAtUtc { get; set; }

    // concurrency token. מוחלף ב-SaveChangesAsync של ה-DbContext
    public Guid Version { get; set; } = Guid.NewGuid();

    public Player Host { get; set; } = null!;
    public Player? Guest { get; set; }
    public ICollection<SessionPlayer> Players { get; set; } = new List<SessionPlayer>();
    public ICollection<GameItem> Items { get; set; } = new List<GameItem>();
}