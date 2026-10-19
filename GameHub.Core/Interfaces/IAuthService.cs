using GameHub.Core.DTOs;
using GameHub.Core.Results;

namespace GameHub.Core.Interfaces;

public interface IAuthService
{
    Task<Result<PlayerResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct);

    Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
}