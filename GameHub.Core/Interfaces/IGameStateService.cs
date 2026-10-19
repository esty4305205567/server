using GameHub.Core.DTOs;
using GameHub.Core.Results;

namespace GameHub.Core.Interfaces;

public interface IGameStateService
{
    Task<Result<GameStateResponse>> GetStateAsync(
        int sessionId, int playerId, CancellationToken ct);
}