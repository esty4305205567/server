using GameHub.Core.DTOs;
using GameHub.Core.Enums;
using GameHub.Core.Results;

namespace GameHub.Core.Interfaces;

public interface IGameSessionService
{
    Task<Result<GameSessionResponse>> CreateAsync(GameType gameType, int hostId, CancellationToken ct);

    Task<Result<JoinGameSessionResponse>> JoinAsync(string code, int playerId, CancellationToken ct);
}