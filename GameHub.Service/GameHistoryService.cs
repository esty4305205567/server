using AutoMapper;
using GameHub.Core.DTOs;
using GameHub.Core.Interfaces;
using GameHub.Core.Results;

namespace GameHub.Service;

public class GameHistoryService : IGameHistoryService
{
    private readonly IGameSessionRepository _sessions;
    private readonly IMapper _mapper;

    public GameHistoryService(IGameSessionRepository sessions, IMapper mapper)
    {
        _sessions = sessions;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<GameHistoryItemResponse>>> GetHistoryAsync(
        int playerId, bool isAdmin, GameHistoryQuery query, CancellationToken ct)
    {
        // מנהל רואה את כולם (null), שחקן רואה רק משחקים שהוא משתתף בהם
        int? scope = isAdmin ? null : playerId;

        var sessions = await _sessions.GetHistoryAsync(
            scope, query.GameType, query.SortBy, query.Desc, ct);
        var response = _mapper.Map<List<GameHistoryItemResponse>>(sessions);

        return Result<IReadOnlyList<GameHistoryItemResponse>>.Ok(response);
    }
}