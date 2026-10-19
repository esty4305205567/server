using GameHub.Core.Entities;
using GameHub.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Data.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly AppDbContext _db;

    public PlayerRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Player?> GetByIdAsync(int id, CancellationToken ct)
    {
        return _db.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public Task<Player?> GetByUsernameAsync(string username, CancellationToken ct)
    {
        return _db.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Username == username, ct);
    }

    public async Task<IReadOnlyDictionary<int, string>> GetUsernamesAsync(
        IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        return await _db.Players
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.Username })
            .ToDictionaryAsync(p => p.Id, p => p.Username, ct);
    }

    public async Task AddAsync(Player player, CancellationToken ct)
    {
        await _db.Players.AddAsync(player, ct);
    }
}