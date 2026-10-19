using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Service;

namespace GameHub.Tests;

public class NumberRaceItemGeneratorTests
{
    private const int ExpectedRounds = 50;
    private const int DistractorRange = 5;

    private static IReadOnlyList<GameItem> Generate(int seed = 1) =>
        new NumberRaceItemGenerator(new Random(seed)).Generate(new GameSession());

    private static int[] ParseOptions(GameItem item) =>
        item.Options!.Split(',').Select(int.Parse).ToArray();

    [Fact]
    public void Type_IsNumberRace()
    {
        Assert.Equal(GameType.NumberRace, new NumberRaceItemGenerator(new Random(1)).Type);
    }

    [Fact]
    public void Generate_CreatesFiftyRoundsWithSequentialOrder()
    {
        var items = Generate();

        Assert.Equal(ExpectedRounds, items.Count);
        Assert.Equal(Enumerable.Range(1, ExpectedRounds), items.Select(i => i.Order));
    }

    [Fact]
    public void Generate_TheTargetNumberIsTheRoundNumber()
    {
        foreach (var item in Generate())
        {
            Assert.Equal(item.Order.ToString(), item.CorrectAnswer);
            Assert.Contains(item.Order.ToString(), item.DisplayText);
        }
    }

    [Fact]
    public void Generate_EachRoundHasFiveDistinctOptionsIncludingTheCorrectOne()
    {
        foreach (var item in Generate())
        {
            var options = ParseOptions(item);

            Assert.Equal(5, options.Length);
            Assert.Equal(5, options.Distinct().Count());
            Assert.Contains(item.Order, options);
        }
    }

    [Fact]
    public void Generate_DistractorsArePositiveAndCloseToTheTarget()
    {
        foreach (var item in Generate())
        {
            var distractors = ParseOptions(item).Where(o => o != item.Order);

            Assert.All(distractors, d =>
            {
                Assert.True(d > 0, $"Round {item.Order} has a non-positive option {d}");
                Assert.InRange(d, item.Order - DistractorRange, item.Order + DistractorRange);
            });
        }
    }

    [Fact]
    public void Generate_EachRoundIsWorthThreePoints()
    {
        Assert.All(Generate(), item => Assert.Equal(3, item.Points));
    }

    [Fact]
    public void Generate_SameSeed_ProducesSameOptions()
    {
        var first = Generate(seed: 7).Select(i => i.Options);
        var second = Generate(seed: 7).Select(i => i.Options);

        Assert.Equal(first, second);
    }
}