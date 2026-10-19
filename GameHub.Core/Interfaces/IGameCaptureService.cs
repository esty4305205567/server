using GameHub.Core.DTOs;
using GameHub.Core.Results;

namespace GameHub.Core.Interfaces;

public interface IGameCaptureService
{
    Task<Result<CaptureResponse>> CaptureAsync(
        int sessionId, int playerId, CaptureRequest request, CancellationToken ct);
}