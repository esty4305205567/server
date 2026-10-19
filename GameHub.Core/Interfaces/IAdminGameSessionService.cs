using GameHub.Core.DTOs;
using GameHub.Core.Results;

namespace GameHub.Core.Interfaces;

public interface IAdminGameSessionService
{
    Task<Result<IReadOnlyList<AdminGameSessionResponse>>> GetAllAsync(
        AdminGameSessionsQuery query, CancellationToken ct);
}