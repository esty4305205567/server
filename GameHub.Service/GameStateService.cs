using AutoMapper;
using GameHub.Core.DTOs;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using GameHub.Core.Results;

namespace GameHub.Service;

public class GameStateService : IGameStateService
{
    private readonly IGameSessionRepository _sessions;
    private readonly IGameItemRepository _items;
    private readonly IPlayerRepository _players;
    private readonly IGameFinisher _finisher;
    private readonly IMapper _mapper;
    private readonly IClock _clock;

    public GameStateService(
        IGameSessionRepository sessions,
        IGameItemRepository items,
        IPlayerRepository players,
        IGameFinisher finisher,
        IMapper mapper,
        IClock clock)
    {
        _sessions = sessions;
        _items = items;
        _players = players;
        _finisher = finisher;
        _mapper = mapper;
        _clock = clock;
    }

    public async Task<Result<GameStateResponse>> GetStateAsync(
        int sessionId, int playerId, CancellationToken ct)
    {
        var session = await _sessions.GetByIdAsync(sessionId, ct);
        if (session is null)
        {
            return Result<GameStateResponse>.NotFound("המשחק לא נמצא");
        }

        if (session.HostId != playerId && session.GuestId != playerId)
        {
            return Result<GameStateResponse>.Forbidden("אינך משתתף במשחק הזה");
        }

        var now = _clock.UtcNow;
        session = await _finisher.FinishIfExpiredAsync(session, ct);

        var opponentId = session.HostId == playerId ? session.GuestId : session.HostId;
        var playerIds = opponentId is null
            ? new[] { playerId }
            : new[] { playerId, opponentId.Value };
        var names = await _players.GetUsernamesAsync(playerIds, ct);

        var state = new GameStateResponse
        {
            Code = session.Code,
            Status = session.Status,
            Type = session.Type,
            ServerNowUtc = now
        };

        if (session.Status == SessionStatus.Waiting)
        {
            state.Me = new PlayerStateResponse { Username = NameOf(names, playerId) };
            return Result<GameStateResponse>.Ok(state);
        }

        var points = await _items.GetPointsByPlayerAsync(session.Id, ct);
        state.Me = BuildPlayer(names, points, playerId);
        state.Opponent = opponentId is null
            ? null
            : BuildPlayer(names, points, opponentId.Value);

        if (session.Status == SessionStatus.Finished)
        {
            var winnerId = session.Players.FirstOrDefault(p => p.IsWinner)?.PlayerId;

            state.RemainingSeconds = 0;
            state.Winner = winnerId is null ? null : NameOf(names, winnerId.Value);
            state.IsDraw = winnerId is null;

            return Result<GameStateResponse>.Ok(state);
        }

        var startedAt = session.StartedAtUtc!.Value;
        var remaining = (startedAt.AddSeconds(session.DurationSeconds) - now).TotalSeconds;
        state.RemainingSeconds = Math.Max(0, (int)Math.Ceiling(remaining));

        await FillActiveItemsAsync(state, session, startedAt, now, ct);

        return Result<GameStateResponse>.Ok(state);
    }

    private async Task FillActiveItemsAsync(
        GameStateResponse state, GameSession session, DateTime startedAt, DateTime now, CancellationToken ct)
    {
        if (session.Type == GameType.LootCollection)
        {
            var elapsedMs = Math.Max(0, (int)(now - startedAt).TotalMilliseconds);
            var boxes = await _items.GetActiveLootItemsAsync(session.Id, elapsedMs, ct);

            state.LootBoxes = _mapper.Map<List<LootBoxResponse>>(boxes);
            return;
        }

        var round = await _items.GetCurrentRoundItemAsync(session.Id, ct);

        state.CurrentRound = round is null ? null : _mapper.Map<RoundResponse>(round);
    }

    private static PlayerStateResponse BuildPlayer(
        IReadOnlyDictionary<int, string> names, IReadOnlyDictionary<int, int> points, int playerId)
    {
        return new PlayerStateResponse
        {
            Username = NameOf(names, playerId),
            Score = points.GetValueOrDefault(playerId)
        };
    }

    private static string NameOf(IReadOnlyDictionary<int, string> names, int playerId)
    {
        return names.TryGetValue(playerId, out var name)
            ? name
            : throw new InvalidOperationException($"Player {playerId} has no username.");
    }
}