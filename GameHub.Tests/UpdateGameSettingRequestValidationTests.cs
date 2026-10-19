using System.ComponentModel.DataAnnotations;
using GameHub.Core.DTOs;
using GameHub.Core.Validation;

namespace GameHub.Tests;

public class UpdateGameSettingRequestValidationTests
{
    private static List<ValidationResult> Validate(UpdateGameSettingRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void DurationAndSpeedInRange_HasNoErrors()
    {
        Assert.Empty(Validate(new UpdateGameSettingRequest { DurationSeconds = 60, Speed = 3 }));
    }

    [Fact]
    public void SpeedOmitted_HasNoErrors()
    {
        Assert.Empty(Validate(new UpdateGameSettingRequest { DurationSeconds = 60, Speed = null }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(29)]
    [InlineData(601)]
    public void DurationOutOfRange_ReportsHebrewDurationMessage(int duration)
    {
        var errors = Validate(new UpdateGameSettingRequest { DurationSeconds = duration, Speed = 3 });

        var error = Assert.Single(errors);
        Assert.Equal(GameSettingRules.DurationOutOfRangeMessage, error.ErrorMessage);
        Assert.Contains(nameof(UpdateGameSettingRequest.DurationSeconds), error.MemberNames);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(600)]
    public void DurationAtTheBoundaries_IsValid(int duration)
    {
        Assert.Empty(Validate(new UpdateGameSettingRequest { DurationSeconds = duration, Speed = 3 }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public void SpeedOutOfRange_ReportsHebrewSpeedMessage(int speed)
    {
        var errors = Validate(new UpdateGameSettingRequest { DurationSeconds = 60, Speed = speed });

        var error = Assert.Single(errors);
        Assert.Equal(GameSettingRules.SpeedOutOfRangeMessage, error.ErrorMessage);
        Assert.Contains(nameof(UpdateGameSettingRequest.Speed), error.MemberNames);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void SpeedAtTheBoundaries_IsValid(int speed)
    {
        Assert.Empty(Validate(new UpdateGameSettingRequest { DurationSeconds = 60, Speed = speed }));
    }
}