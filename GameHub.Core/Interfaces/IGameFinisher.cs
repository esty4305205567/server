using GameHub.Core.DTOs;
using GameHub.Core.Entities;

namespace GameHub.Core.Interfaces;

public interface IGameFinisher
{
    Task FinishAsync(GameSession session, PendingCapture? pendingCapture, CancellationToken ct);

    Task<GameSession> FinishIfExpiredAsync(GameSession session, CancellationToken ct);
}