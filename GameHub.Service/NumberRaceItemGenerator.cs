using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;

namespace GameHub.Service;

public class NumberRaceItemGenerator : IGameItemGenerator
{
    private const int RoundCount = 50;
    private const int DistractorCount = 4;
    private const int DistractorRange = 5;

    private readonly Random _random;

    public NumberRaceItemGenerator(Random random)
    {
        _random = random;
    }

    public GameType Type => GameType.NumberRace;

    public IReadOnlyList<GameItem> Generate(GameSession session)
    {
        var items = new List<GameItem>();

        for (var round = 1; round <= RoundCount; round++)
        {
            var candidates = new List<int>();

            for (var value = round - DistractorRange; value <= round + DistractorRange; value++)
            {
                if (value > 0 && value != round)
                {
                    candidates.Add(value);
                }
            }

            var options = candidates
                .OrderBy(_ => _random.Next())
                .Take(DistractorCount)
                .Append(round)
                .OrderBy(_ => _random.Next())
                .ToList();

            items.Add(new GameItem
            {
                Order = round,
                DisplayText = $"לחצו על {round}",
                CorrectAnswer = round.ToString(),
                Options = string.Join(",", options),
                Points = 3
            });
        }

        return items;
    }
}