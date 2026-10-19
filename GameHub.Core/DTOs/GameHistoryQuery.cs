using System.ComponentModel.DataAnnotations;
using GameHub.Core.Enums;

namespace GameHub.Core.DTOs;

public class GameHistoryQuery
{
    [EnumDataType(typeof(GameType))]
    public GameType? GameType { get; set; }

    [EnumDataType(typeof(GameHistorySortBy))]
    public GameHistorySortBy SortBy { get; set; } = GameHistorySortBy.Date;

    public bool Desc { get; set; } = true;
}