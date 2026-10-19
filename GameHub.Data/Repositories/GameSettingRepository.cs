using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Data.Repositories;

public class GameSettingRepository : IGameSettingRepository
{
    private readonly AppDbContext _db;

    public GameSettingRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<GameSetting?> GetAsync(GameType gameType, CancellationToken ct)
    {
        return _db.GameSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.GameType == gameType, ct);
    }

    public async Task<IReadOnlyList<GameSetting>> GetAllAsync(CancellationToken ct)
    {
        return await _db.GameSettings
            .AsNoTracking()
            .OrderBy(g => g.GameType)
            .ToListAsync(ct);
    }

    public Task<GameSetting?> GetForUpdateAsync(GameType gameType, CancellationToken ct)
    {
        return _db.GameSettings
            .FirstOrDefaultAsync(g => g.GameType == gameType, ct);
    }
}