using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;

namespace GameHub.Service;

public class LootItemGenerator : IGameItemGenerator
{
    private const int BoardSize = 15;
    private const int SecondsPerBox = 2;


    private readonly Random _random;

    public LootItemGenerator(Random random)
    {
        _random = random;
    }

    public GameType Type => GameType.LootCollection;

    public IReadOnlyList<GameItem> Generate(GameSession session)
    {
        var displayMs = session.DisplayMs
            ?? throw new InvalidOperationException(
               "GameSession.DisplayMs must be set before generating loot items."); var boxesPerSlot = Math.Max(1, (session.DurationSeconds + SecondsPerBox - 1) / SecondsPerBox);

        var items = new List<GameItem>();

        for (var slot = 0; slot < BoardSize; slot++)
        {
            var start = 0;
            var duration = displayMs + slot * displayMs / BoardSize;

            for (var n = 0; n < boxesPerSlot; n++)
            {
                items.Add(new GameItem
                {
                    DisplayText = string.Empty,
                    Points = _random.Next(20, 201),
                    X = Math.Round(5 + _random.NextDouble() * 90, 1),
                    Y = Math.Round(5 + _random.NextDouble() * 90, 1),
                    AppearAtMs = start,
                    DisappearAtMs = start + duration,
                    Slot = slot
                });

                start += duration;
                duration = displayMs;
            }
        }

        var ordered = items.OrderBy(i => i.AppearAtMs).ToList();

        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].Order = index + 1;
        }

        return ordered;
    }
}