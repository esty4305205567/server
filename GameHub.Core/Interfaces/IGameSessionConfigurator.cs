using GameHub.Core.Entities;
using GameHub.Core.Enums;

namespace GameHub.Core.Interfaces;

public interface IGameSessionConfigurator
{
    GameType Type { get; }

    void Configure(GameSession session);
}