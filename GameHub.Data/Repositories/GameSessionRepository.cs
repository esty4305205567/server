using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Data.Repositories;

public class GameSessionRepository : IGameSessionRepository
{
    // בלי דפדוף, אסור להחזיר את כל הטבלה
    private const int MaxAdminRows = 200;
    private const int MaxHistoryRows = 200;

    private readonly AppDbContext _db;

    public GameSessionRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<GameSession?> GetByCodeAsync(string code, CancellationToken ct)
    {
        return _db.GameSessions
            .Include(s => s.Players)
            .Include(s => s.Host)
            .FirstOrDefaultAsync(s => s.Code == code, ct);
    }

    public Task<GameSession?> GetByIdAsync(int sessionId, CancellationToken ct)
    {
        return _db.GameSessions
            .Include(s => s.Players)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);
    }

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct)
    {
        return _db.GameSessions
            .AsNoTracking()
            .AnyAsync(s => s.Code == code, ct);
    }

    public async Task<IReadOnlyList<GameSession>> GetAllAsync(
        GameType? type, SessionStatus? status, CancellationToken ct)
    {
        var query = _db.GameSessions.AsNoTracking();

        if (type.HasValue)
            query = query.Where(s => s.Type == type.Value);
        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        return await query
            .Include(s => s.Host)
            .Include(s => s.Guest)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ThenByDescending(s => s.Id)
            .Take(MaxAdminRows)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GameSession>> GetHistoryAsync(
        int? playerId, GameType? type, GameHistorySortBy sortBy, bool desc, CancellationToken ct)
    {
        var query = _db.GameSessions
            .AsNoTracking()
            .Where(s => s.Status == SessionStatus.Finished);

        if (playerId.HasValue)
            query = query.Where(s => s.Players.Any(p => p.PlayerId == playerId.Value));
        if (type.HasValue)
            query = query.Where(s => s.Type == type.Value);

        // ניקוד המשחק הוא הניקוד הגבוה מבין שני השחקנים. ה-cast ל-int? מונע שגיאה על קבוצה ריקה
        IOrderedQueryable<GameSession> ordered = sortBy == GameHistorySortBy.Score
            ? (desc
                ? query.OrderByDescending(s => s.Players.Max(p => (int?)p.Score))
                : query.OrderBy(s => s.Players.Max(p => (int?)p.Score)))
            : (desc
                ? query.OrderByDescending(s => s.FinishedAtUtc)
                : query.OrderBy(s => s.FinishedAtUtc));

        // מפתח מיון שני ליציבות
        ordered = desc
            ? ordered.ThenByDescending(s => s.Id)
            : ordered.ThenBy(s => s.Id);

        return await ordered
            .Include(s => s.Host)
            .Include(s => s.Guest)
            .Include(s => s.Players)
                .ThenInclude(p => p.Player)
            .Take(MaxHistoryRows)
            .ToListAsync(ct);
    }

    public async Task AddAsync(GameSession session, CancellationToken ct)
    {
        await _db.GameSessions.AddAsync(session, ct);
    }

    public void Detach(GameSession session)
    {
        foreach (var player in session.Players.ToList())
        {
            _db.Entry(player).State = EntityState.Detached;
        }

        foreach (var item in session.Items.ToList())
        {
            _db.Entry(item).State = EntityState.Detached;
        }

        _db.Entry(session).State = EntityState.Detached;
    }
}