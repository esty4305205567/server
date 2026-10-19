using GameHub.Core.Enums;

namespace GameHub.Core.DTOs;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public PlayerRole Role { get; set; }
}