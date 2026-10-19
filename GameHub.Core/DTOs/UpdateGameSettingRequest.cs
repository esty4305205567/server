using System.ComponentModel.DataAnnotations;
using GameHub.Core.Validation;

namespace GameHub.Core.DTOs;

public class UpdateGameSettingRequest
{
    [Range(
        GameSettingRules.MinDurationSeconds,
        GameSettingRules.MaxDurationSeconds,
        ErrorMessage = GameSettingRules.DurationOutOfRangeMessage)]
    public int DurationSeconds { get; set; }

    [Range(
        GameSettingRules.MinSpeed,
        GameSettingRules.MaxSpeed,
        ErrorMessage = GameSettingRules.SpeedOutOfRangeMessage)]
    public int? Speed { get; set; }
}