using System.ComponentModel.DataAnnotations;

namespace GameHub.Core.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "יש להזין שם משתמש")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "יש להזין סיסמה")]
    public string Password { get; set; } = string.Empty;
}