using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Service;

namespace GameHub.Tests;

public class QuizItemGeneratorTests
{
    private static IReadOnlyList<GameItem> Generate(int seed = 1) =>
        new QuizItemGenerator(new Random(seed)).Generate(new GameSession());

    private static (int Left, char Operator, int Right) Parse(string displayText)
    {
        var parts = displayText.Split(' ');
        return (int.Parse(parts[0]), parts[1][0], int.Parse(parts[2]));
    }

    [Fact]
    public void Type_IsQuickQuiz()
    {
        Assert.Equal(GameType.QuickQuiz, new QuizItemGenerator(new Random(1)).Type);
    }

    [Fact]
    public void Generate_CreatesFortyRoundsWithSequentialOrder()
    {
        var items = Generate();

        Assert.Equal(40, items.Count);
        Assert.Equal(Enumerable.Range(1, 40), items.Select(i => i.Order));
    }

    [Fact]
    public void Generate_EachCorrectAnswerMatchesTheDisplayedExercise()
    {
        foreach (var item in Generate())
        {
            var (left, op, right) = Parse(item.DisplayText);

            var expected = op switch
            {
                '×' => left * right,
                '+' => left + right,
                _ => throw new InvalidOperationException($"Unexpected operator '{op}'")
            };

            Assert.Equal(expected.ToString(), item.CorrectAnswer);
        }
    }

    [Fact]
    public void Generate_OperandsStayInConfiguredRanges()
    {
        foreach (var item in Generate())
        {
            var (left, op, right) = Parse(item.DisplayText);

            var (min, max) = op == '×' ? (2, 12) : (10, 99);

            Assert.InRange(left, min, max);
            Assert.InRange(right, min, max);
        }
    }

    [Fact]
    public void Generate_RoundsAreWorthOnePointAndHaveNoOptions()
    {
        foreach (var item in Generate())
        {
            Assert.Equal(1, item.Points);
            Assert.Null(item.Options);
            Assert.Null(item.CapturedByPlayerId);
        }
    }

    [Fact]
    public void Generate_UsesBothMultiplicationAndAddition()
    {
        var operators = Generate().Select(i => Parse(i.DisplayText).Operator).ToHashSet();

        Assert.Contains('×', operators);
        Assert.Contains('+', operators);
    }

    [Fact]
    public void Generate_SameSeed_ProducesSameExercises()
    {
        var first = Generate(seed: 7).Select(i => i.DisplayText);
        var second = Generate(seed: 7).Select(i => i.DisplayText);

        Assert.Equal(first, second);
    }
}