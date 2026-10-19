using System.ComponentModel.DataAnnotations;
using GameHub.Core.Enums;

namespace GameHub.Core.DTOs;

public class AdminGameSessionsQuery
{
    [EnumDataType(typeof(GameType))]
    public GameType? GameType { get; set; }

    [EnumDataType(typeof(SessionStatus))]
    public SessionStatus? Status { get; set; }
}