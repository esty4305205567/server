using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;

namespace GameHub.Service;

public class LootSessionConfigurator : IGameSessionConfigurator
{
    private readonly ILootTiming _timing;

    public LootSessionConfigurator(ILootTiming timing)
    {
        _timing = timing;
    }

    public GameType Type => GameType.LootCollection;

    public void Configure(GameSession session)
    {
        session.DisplayMs = _timing.GetDisplayMs(session.Speed);
    }
}