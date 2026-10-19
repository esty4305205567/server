using GameHub.Core.DTOs;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Exceptions;
using GameHub.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace GameHub.Service;

public class GameFinisher : IGameFinisher
{
    private readonly IGameItemRepository _items;
    private readonly IGameSessionRepository _sessions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<GameFinisher> _logger;

    public GameFinisher(
        IGameItemRepository items,
        IGameSessionRepository sessions,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<GameFinisher> logger)
    {
        _items = items;
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task<GameSession> FinishIfExpiredAsync(GameSession session, CancellationToken ct)
    {
        if (!IsExpired(session))
        {
            return session;
        }

        await FinishAsync(session, null, ct);

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
            return session;
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning(
                "Concurrency conflict finishing session {SessionId}, reloading final state",
                session.Id);

            _sessions.Detach(session);

            return await _sessions.GetByIdAsync(session.Id, ct)
                ?? throw new InvalidOperationException(
                    $"Session {session.Id} disappeared while finishing.");
        }
    }

    public async Task FinishAsync(
        GameSession session, PendingCapture? pendingCapture, CancellationToken ct)
    {
        if (session.Status == SessionStatus.Finished)
        {
            return;
        }

        var points = new Dictionary<int, int>(
            await _items.GetPointsByPlayerAsync(session.Id, ct));

        if (pendingCapture is not null)
        {
            points.TryGetValue(pendingCapture.PlayerId, out var current);
            points[pendingCapture.PlayerId] = current + pendingCapture.Points;
        }

        foreach (var sessionPlayer in session.Players)
        {
            sessionPlayer.Score = points.GetValueOrDefault(sessionPlayer.PlayerId);
        }

        var topScore = session.Players.Max(p => p.Score);
        var leaders = session.Players.Where(p => p.Score == topScore).ToList();

        foreach (var sessionPlayer in session.Players)
        {
            sessionPlayer.IsWinner = leaders.Count == 1 && sessionPlayer.Score == topScore;
        }

        session.Status = SessionStatus.Finished;
        session.FinishedAtUtc = _clock.UtcNow;
    }

    private bool IsExpired(GameSession session)
    {
        return session.Status == SessionStatus.InProgress
            && session.StartedAtUtc!.Value.AddSeconds(session.DurationSeconds) < _clock.UtcNow;
    }
}