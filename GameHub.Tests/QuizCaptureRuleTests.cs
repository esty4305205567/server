using GameHub.Core.DTOs;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Results;
using GameHub.Service;

namespace GameHub.Tests;

public class QuizCaptureRuleTests
{
    private readonly QuizCaptureRule _rule = new();

    private static CaptureRuleResult Evaluate(string? answer, int currentRound = 5)
    {
        return new QuizCaptureRule().Evaluate(new CaptureContext
        {
            Session = new GameSession { Type = GameType.QuickQuiz },
            Item = new GameItem
            {
                Order = 5,
                DisplayText = "7 × 8",
                CorrectAnswer = "56"
            },
            NowUtc = DateTime.UtcNow,
            Answer = answer,
            CurrentRoundOrder = currentRound
        });
    }

    [Fact]
    public void Evaluate_Type_IsQuickQuiz()
    {
        Assert.Equal(GameType.QuickQuiz, _rule.Type);
    }

    [Theory]
    [InlineData("56")]
    [InlineData("  56  ")]
    public void Evaluate_CorrectAnswer_IsAllowed(string answer)
    {
        Assert.True(Evaluate(answer).IsAllowed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Evaluate_EmptyAnswer_ReturnsBadRequest(string? answer)
    {
        var result = Evaluate(answer);

        Assert.Equal(ResultStatus.BadRequest, result.Status);
        Assert.Equal("יש להקליד תשובה", result.Message);
    }

    [Fact]
    public void Evaluate_FutureRound_ReturnsBadRequest()
    {
        var result = Evaluate("56", currentRound: 4);

        Assert.Equal(ResultStatus.BadRequest, result.Status);
        Assert.Equal("השאלה הזאת עדיין לא התחילה", result.Message);
    }

    [Theory]
    [InlineData("55")]
    [InlineData("056")]
    [InlineData("56.0")]
    public void Evaluate_WrongAnswer_ReturnsBadRequest(string answer)
    {
        var result = Evaluate(answer);

        Assert.Equal(ResultStatus.BadRequest, result.Status);
        Assert.Equal("לא נכון, נסו שוב", result.Message);
    }
}