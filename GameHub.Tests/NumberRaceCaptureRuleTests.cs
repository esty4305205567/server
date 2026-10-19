using GameHub.Core.DTOs;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Results;
using GameHub.Service;

namespace GameHub.Tests;

public class NumberRaceCaptureRuleTests
{
    private readonly NumberRaceCaptureRule _rule = new();

    private static CaptureRuleResult Evaluate(string? answer, int currentRound = 3)
    {
        return new NumberRaceCaptureRule().Evaluate(new CaptureContext
        {
            Session = new GameSession { Type = GameType.NumberRace },
            Item = new GameItem
            {
                Order = 3,
                Options = "3,9,12,5,7",
                CorrectAnswer = "3"
            },
            NowUtc = DateTime.UtcNow,
            Answer = answer,
            CurrentRoundOrder = currentRound
        });
    }

    [Fact]
    public void Evaluate_Type_IsNumberRace()
    {
        Assert.Equal(GameType.NumberRace, _rule.Type);
    }

    [Fact]
    public void Evaluate_CorrectAnswer_IsAllowed()
    {
        Assert.True(Evaluate("3").IsAllowed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Evaluate_EmptyAnswer_ReturnsBadRequest(string? answer)
    {
        var result = Evaluate(answer);

        Assert.Equal(ResultStatus.BadRequest, result.Status);
        Assert.Equal("יש לבחור מספר", result.Message);
    }

    [Fact]
    public void Evaluate_FutureRound_ReturnsBadRequest()
    {
        var result = Evaluate("3", currentRound: 2);

        Assert.Equal(ResultStatus.BadRequest, result.Status);
        Assert.Equal("הסבב הזה עדיין לא התחיל", result.Message);
    }

    [Fact]
    public void Evaluate_AnswerNotInOptions_ReturnsBadRequest()
    {
        var result = Evaluate("99");

        Assert.Equal(ResultStatus.BadRequest, result.Status);
        Assert.Equal("המספר אינו חלק מהסבב", result.Message);
    }

    [Fact]
    public void Evaluate_WrongOption_ReturnsBadRequest()
    {
        var result = Evaluate("9");

        Assert.Equal(ResultStatus.BadRequest, result.Status);
        Assert.Equal("לא נכון, נסו שוב", result.Message);
    }
}