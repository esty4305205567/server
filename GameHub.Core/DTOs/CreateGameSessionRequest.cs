using System.ComponentModel.DataAnnotations;
using GameHub.Core.Enums;

namespace GameHub.Core.DTOs;

public class CreateGameSessionRequest
{
    [Required(ErrorMessage = "יש לבחור סוג משחק")]
    [EnumDataType(typeof(GameType), ErrorMessage = "סוג המשחק אינו חוקי")]
    public GameType? GameType { get; set; }
}