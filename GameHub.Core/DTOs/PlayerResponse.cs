using GameHub.Core.Enums;

namespace GameHub.Core.DTOs;

public class PlayerResponse
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public PlayerRole Role { get; set; }
}