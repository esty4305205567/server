using GameHub.Core.DTOs;
using GameHub.Core.Enums;
using GameHub.Core.Results;

namespace GameHub.Core.Interfaces;

public interface IGameSettingService
{
    Task<Result<IReadOnlyList<GameSettingResponse>>> GetAllAsync(CancellationToken ct);

    Task<Result<GameSettingResponse>> UpdateAsync(
        GameType gameType, UpdateGameSettingRequest request, CancellationToken ct);
}