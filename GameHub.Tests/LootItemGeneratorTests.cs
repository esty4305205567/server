using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Service;

namespace GameHub.Tests;

public class LootItemGeneratorTests
{
    private const int DisplayMs = 4000;
    private const int DurationSeconds = 60;
    private const int BoardSize = 15;

    private static LootItemGenerator CreateGenerator()
    {
        return new LootItemGenerator(new Random(42));
    }

    private static GameSession CreateSession(int? displayMs)
    {
        return new GameSession
        {
            Type = GameType.LootCollection,
            DurationSeconds = DurationSeconds,
            Speed = 4,
            DisplayMs = displayMs
        };
    }

    [Fact]
    public void Generate_DisplayMsMissing_Throws()
    {
        var generator = CreateGenerator();
        var session = CreateSession(null);

        Assert.Throws<InvalidOperationException>(() => generator.Generate(session));
    }

    [Fact]
    public void Generate_EveryBoxIsDisplayedAtLeastMinimum()
    {
        var generator = CreateGenerator();
        var session = CreateSession(DisplayMs);

        var items = generator.Generate(session);

        Assert.All(items, i =>
            Assert.True(i.DisappearAtMs!.Value - i.AppearAtMs!.Value >= 3000));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1500)]
    [InlineData(30000)]
    [InlineData(59000)]
    public void Generate_AtAnyMoment_EverySlotHasExactlyOneVisibleBox(int momentMs)
    {
        var generator = CreateGenerator();
        var session = CreateSession(DisplayMs);

        var items = generator.Generate(session);

        var visible = items
            .Where(i => i.AppearAtMs <= momentMs && momentMs < i.DisappearAtMs)
            .ToList();

        Assert.Equal(BoardSize, visible.Count);
        Assert.Equal(BoardSize, visible.Select(i => i.Slot).Distinct().Count());
    }
}