using System.ComponentModel.DataAnnotations;
using GameHub.Core.Validation;

namespace GameHub.Core.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "יש להזין שם משתמש")]
    [RegularExpression(UsernameRules.Pattern, ErrorMessage = UsernameRules.InvalidMessage)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "יש להזין סיסמה")]
    [MinLength(PasswordRules.MinLength, ErrorMessage = PasswordRules.TooShortMessage)]
    [MaxLength(PasswordRules.MaxLength, ErrorMessage = PasswordRules.TooLongMessage)]
    public string Password { get; set; } = string.Empty;
}