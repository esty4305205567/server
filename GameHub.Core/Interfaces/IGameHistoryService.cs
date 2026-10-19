using GameHub.Core.DTOs;
using GameHub.Core.Results;

namespace GameHub.Core.Interfaces;

public interface IGameHistoryService
{
    Task<Result<IReadOnlyList<GameHistoryItemResponse>>> GetHistoryAsync(
        int playerId, bool isAdmin, GameHistoryQuery query, CancellationToken ct);
}