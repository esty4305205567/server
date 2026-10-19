using GameHub.Core.DTOs;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Results;
using GameHub.Service;

namespace GameHub.Tests;

public class LootCaptureRuleTests
{
    private static readonly DateTime Start = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    private readonly LootCaptureRule _rule = new();

    private static GameSession Session() => new()
    {
        Type = GameType.LootCollection,
        StartedAtUtc = Start
    };

    private static GameItem Item() => new()
    {
        AppearAtMs = 2000,
        DisappearAtMs = 6000
    };

    private CaptureRuleResult Evaluate(int elapsedMs)
    {
        return _rule.Evaluate(new CaptureContext
        {
            Session = Session(),
            Item = Item(),
            NowUtc = Start.AddMilliseconds(elapsedMs)
        });
    }

    [Fact]
    public void Evaluate_Type_IsLootCollection()
    {
        Assert.Equal(GameType.LootCollection, _rule.Type);
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(4000)]
    [InlineData(5999)]
    public void Evaluate_InsideWindow_IsAllowed(int elapsedMs)
    {
        var result = Evaluate(elapsedMs);

        Assert.True(result.IsAllowed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1999)]
    public void Evaluate_BeforeWindow_ReturnsConflict(int elapsedMs)
    {
        var result = Evaluate(elapsedMs);

        Assert.False(result.IsAllowed);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal("התיבה עדיין לא הופיעה", result.Message);
    }

    [Theory]
    [InlineData(6000)]
    [InlineData(9000)]
    public void Evaluate_AfterWindow_ReturnsConflict(int elapsedMs)
    {
        var result = Evaluate(elapsedMs);

        Assert.False(result.IsAllowed);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal("התיבה כבר נעלמה", result.Message);
    }
}