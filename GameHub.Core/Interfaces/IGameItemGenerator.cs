using GameHub.Core.Entities;
using GameHub.Core.Enums;

namespace GameHub.Core.Interfaces;

public interface IGameItemGenerator
{
    GameType Type { get; }

    IReadOnlyList<GameItem> Generate(GameSession session);
}