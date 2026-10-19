using AutoMapper;
using GameHub.Core.DTOs;
using GameHub.Core.Interfaces;
using GameHub.Core.Results;

namespace GameHub.Service;

public class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _players;
    private readonly IMapper _mapper;

    public PlayerService(IPlayerRepository players, IMapper mapper)
    {
        _players = players;
        _mapper = mapper;
    }

    public async Task<Result<PlayerResponse>> GetByIdAsync(
        int requestedPlayerId, int currentPlayerId, bool isAdmin, CancellationToken ct)
    {
        if (!isAdmin && requestedPlayerId != currentPlayerId)
        {
            return Result<PlayerResponse>.Forbidden("אין לך הרשאה לצפות בחשבון הזה");
        }

        var player = await _players.GetByIdAsync(requestedPlayerId, ct);
        if (player is null)
        {
            return Result<PlayerResponse>.NotFound("השחקן לא נמצא");
        }

        return Result<PlayerResponse>.Ok(_mapper.Map<PlayerResponse>(player));
    }
}