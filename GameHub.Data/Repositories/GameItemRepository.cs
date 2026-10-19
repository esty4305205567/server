using GameHub.Core.Entities;
using GameHub.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Data.Repositories;

public class GameItemRepository : IGameItemRepository
{
    private readonly AppDbContext _db;

    public GameItemRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<GameItem?> GetByIdInSessionAsync(int sessionId, int itemId, CancellationToken ct)
    {
        return _db.GameItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.SessionId == sessionId, ct);
    }

    public Task<bool> AnyOtherUncapturedAsync(int sessionId, int excludedItemId, CancellationToken ct)
    {
        return _db.GameItems
            .AsNoTracking()
            .AnyAsync(i => i.SessionId == sessionId
                           && i.Id != excludedItemId
                           && i.CapturedByPlayerId == null, ct);
    }

    public Task<int?> GetCurrentRoundOrderAsync(int sessionId, CancellationToken ct)
    {
        return _db.GameItems
            .AsNoTracking()
            .Where(i => i.SessionId == sessionId && i.CapturedByPlayerId == null)
            .Select(i => (int?)i.Order)
            .MinAsync(ct);
    }

    public async Task<IReadOnlyDictionary<int, int>> GetPointsByPlayerAsync(
        int sessionId, CancellationToken ct)
    {
        return await _db.GameItems
            .AsNoTracking()
            .Where(i => i.SessionId == sessionId && i.CapturedByPlayerId != null)
            .GroupBy(i => i.CapturedByPlayerId!.Value)
            .Select(g => new { PlayerId = g.Key, Points = g.Sum(i => i.Points) })
            .ToDictionaryAsync(x => x.PlayerId, x => x.Points, ct);
    }

    public async Task<IReadOnlyList<GameItem>> GetActiveLootItemsAsync(
        int sessionId, int elapsedMs, CancellationToken ct)
    {
        return await _db.GameItems
            .AsNoTracking()
            .Where(i => i.SessionId == sessionId
                        && i.CapturedByPlayerId == null
                        && i.AppearAtMs <= elapsedMs
                        && i.DisappearAtMs > elapsedMs)
            .OrderBy(i => i.Order)
            .ToListAsync(ct);
    }

    public Task<GameItem?> GetCurrentRoundItemAsync(int sessionId, CancellationToken ct)
    {
        return _db.GameItems
            .AsNoTracking()
            .Where(i => i.SessionId == sessionId && i.CapturedByPlayerId == null)
            .OrderBy(i => i.Order)
            .FirstOrDefaultAsync(ct);
    }
}