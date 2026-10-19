using GameHub.Core.Entities;

namespace GameHub.Core.Interfaces;

public interface IGameItemRepository
{
    Task<GameItem?> GetByIdInSessionAsync(int sessionId, int itemId, CancellationToken ct);
    Task<bool> AnyOtherUncapturedAsync(int sessionId, int excludedItemId, CancellationToken ct);
    Task<int?> GetCurrentRoundOrderAsync(int sessionId, CancellationToken ct);
    Task<IReadOnlyDictionary<int, int>> GetPointsByPlayerAsync(int sessionId, CancellationToken ct);

    Task<IReadOnlyList<GameItem>> GetActiveLootItemsAsync(
        int sessionId, int elapsedMs, CancellationToken ct);

    Task<GameItem?> GetCurrentRoundItemAsync(int sessionId, CancellationToken ct);
}