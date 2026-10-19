using GameHub.Core.DTOs;
using GameHub.Core.Results;

namespace GameHub.Core.Interfaces;

public interface IPlayerService
{
    Task<Result<PlayerResponse>> GetByIdAsync(
        int requestedPlayerId, int currentPlayerId, bool isAdmin, CancellationToken ct);
}