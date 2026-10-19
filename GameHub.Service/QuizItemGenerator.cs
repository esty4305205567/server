using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;

namespace GameHub.Service;

public class QuizItemGenerator : IGameItemGenerator
{
    private const int RoundCount = 40;
    private const int PointsPerRound = 1;

    private const int MultiplyMin = 2;
    private const int MultiplyMax = 12;

    private const int AddMin = 10;
    private const int AddMax = 99;

    private readonly Random _random;

    public QuizItemGenerator(Random random)
    {
        _random = random;
    }

    public GameType Type => GameType.QuickQuiz;

    public IReadOnlyList<GameItem> Generate(GameSession session)
    {
        var items = new List<GameItem>(RoundCount);

        for (var round = 1; round <= RoundCount; round++)
        {
            items.Add(CreateRound(round));
        }

        return items;
    }

    private GameItem CreateRound(int order)
    {
        var (text, answer) = _random.Next(2) == 0
            ? CreateMultiplication()
            : CreateAddition();

        return new GameItem
        {
            Order = order,
            DisplayText = text,
            CorrectAnswer = answer.ToString(),
            Points = PointsPerRound
        };
    }

    private (string Text, int Answer) CreateMultiplication()
    {
        var left = _random.Next(MultiplyMin, MultiplyMax + 1);
        var right = _random.Next(MultiplyMin, MultiplyMax + 1);

        return ($"{left} × {right}", left * right);
    }

    private (string Text, int Answer) CreateAddition()
    {
        var left = _random.Next(AddMin, AddMax + 1);
        var right = _random.Next(AddMin, AddMax + 1);

        return ($"{left} + {right}", left + right);
    }
}