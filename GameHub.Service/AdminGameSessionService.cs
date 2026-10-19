using AutoMapper;
using GameHub.Core.DTOs;
using GameHub.Core.Interfaces;
using GameHub.Core.Results;

namespace GameHub.Service;

public class AdminGameSessionService : IAdminGameSessionService
{
    private readonly IGameSessionRepository _sessions;
    private readonly IMapper _mapper;

    public AdminGameSessionService(IGameSessionRepository sessions, IMapper mapper)
    {
        _sessions = sessions;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<AdminGameSessionResponse>>> GetAllAsync(
        AdminGameSessionsQuery query, CancellationToken ct)
    {
        var sessions = await _sessions.GetAllAsync(query.GameType, query.Status, ct);
        var response = _mapper.Map<List<AdminGameSessionResponse>>(sessions);

        return Result<IReadOnlyList<AdminGameSessionResponse>>.Ok(response);
    }
}