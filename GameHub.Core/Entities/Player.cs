using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GameHub.Core.Enums;

namespace GameHub.Core.Entities;

public class Player
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public PlayerRole Role { get; set; } = PlayerRole.Player;

    public ICollection<SessionPlayer> Sessions { get; set; } = new List<SessionPlayer>();
}