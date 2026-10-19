using GameHub.Core.Entities;
using GameHub.Core.Enums;

namespace GameHub.Core.Interfaces;

public interface IGameSettingRepository
{
    Task<GameSetting?> GetAsync(GameType gameType, CancellationToken ct);

    Task<IReadOnlyList<GameSetting>> GetAllAsync(CancellationToken ct);

    Task<GameSetting?> GetForUpdateAsync(GameType gameType, CancellationToken ct);
}