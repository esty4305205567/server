using GameHub.Core.Entities;
using GameHub.Core.Enums;

namespace GameHub.Core.Interfaces;

public interface IGameSessionRepository
{
    Task<GameSession?> GetByCodeAsync(string code, CancellationToken ct);
    Task<GameSession?> GetByIdAsync(int sessionId, CancellationToken ct);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct);

    // רשימת ניהול: קריאה בלבד, עם סינון אופציונלי ותקרה קשיחה על מספר השורות
    Task<IReadOnlyList<GameSession>> GetAllAsync(
        GameType? type, SessionStatus? status, CancellationToken ct);

    // היסטוריה: משחקים שהסתיימו בלבד. playerId null = כל המשחקים (מנהל)
    Task<IReadOnlyList<GameSession>> GetHistoryAsync(
        int? playerId, GameType? type, GameHistorySortBy sortBy, bool desc, CancellationToken ct);

    Task AddAsync(GameSession session, CancellationToken ct);

    void Detach(GameSession session);
}