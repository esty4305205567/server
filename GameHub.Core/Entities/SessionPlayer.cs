using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameHub.Core.Entities;

// ישות הביניים של ה-many-to-many בין Player ל-GameSession
public class SessionPlayer
{
    public int SessionId { get; set; }
    public int PlayerId { get; set; }

    // נכתב פעם אחת בסיום המשחק, ולא מתעדכן בכל תפיסה
    public int Score { get; set; }
    public bool IsWinner { get; set; }

    public GameSession Session { get; set; } = null!;
    public Player Player { get; set; } = null!;
}
