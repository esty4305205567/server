using GameHub.Core.DTOs;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Exceptions;
using GameHub.Core.Interfaces;
using GameHub.Core.Results;
using Microsoft.Extensions.Logging;

namespace GameHub.Service;

public class GameCaptureService : IGameCaptureService
{
    private readonly IGameSessionRepository _sessions;
    private readonly IGameItemRepository _items;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGameFinisher _finisher;
    private readonly IReadOnlyCollection<ICaptureRule> _rules;
    private readonly IClock _clock;
    private readonly ILogger<GameCaptureService> _logger;

    public GameCaptureService(
        IGameSessionRepository sessions,
        IGameItemRepository items,
        IUnitOfWork unitOfWork,
        IGameFinisher finisher,
        IEnumerable<ICaptureRule> rules,
        IClock clock,
        ILogger<GameCaptureService> logger)
    {
        _sessions = sessions;
        _items = items;
        _unitOfWork = unitOfWork;
        _finisher = finisher;
        _rules = rules.ToList();
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<CaptureResponse>> CaptureAsync(
        int sessionId, int playerId, CaptureRequest request, CancellationToken ct)
    {
        var session = await _sessions.GetByIdAsync(sessionId, ct);
        if (session is null)
        {
            return Result<CaptureResponse>.NotFound("המשחק לא נמצא");
        }

        var item = await _items.GetByIdInSessionAsync(sessionId, request.ItemId, ct);
        if (item is null)
        {
            return Result<CaptureResponse>.NotFound("הפריט לא נמצא");
        }

        if (session.HostId != playerId && session.GuestId != playerId)
        {
            return Result<CaptureResponse>.Forbidden("אינך משתתף במשחק הזה");
        }

        if (session.Status != SessionStatus.InProgress)
        {
            return Result<CaptureResponse>.Conflict("המשחק אינו פעיל");
        }

        var now = _clock.UtcNow;

        session = await _finisher.FinishIfExpiredAsync(session, ct);
        if (session.Status == SessionStatus.Finished)
        {
            return Result<CaptureResponse>.Conflict("המשחק הסתיים");
        }

        if (item.CapturedByPlayerId is not null)
        {
            return Result<CaptureResponse>.Conflict("היריב הקדים אותך, הפריט כבר נתפס");
        }

        var rule = _rules.FirstOrDefault(r => r.Type == session.Type);
        if (rule is null)
        {
            _logger.LogError("No capture rule registered for game type {GameType}", session.Type);
            return Result<CaptureResponse>.NotFound("סוג המשחק אינו נתמך");
        }

        var currentRoundOrder = session.Type == GameType.LootCollection
            ? null
            : await _items.GetCurrentRoundOrderAsync(sessionId, ct);

        var verdict = rule.Evaluate(new CaptureContext
        {
            Session = session,
            Item = item,
            NowUtc = now,
            Answer = request.Answer,
            CurrentRoundOrder = currentRoundOrder
        });
        if (!verdict.IsAllowed)
        {
            return verdict.Status == ResultStatus.Conflict
                ? Result<CaptureResponse>.Conflict(verdict.Message!)
                : Result<CaptureResponse>.BadRequest(verdict.Message!);
        }

        item.CapturedByPlayerId = playerId;
        item.CapturedAtUtc = now;

        var gameFinished = false;
        if (session.Type != GameType.LootCollection
            && !await _items.AnyOtherUncapturedAsync(sessionId, item.Id, ct))
        {
            await _finisher.FinishAsync(session, new PendingCapture(playerId, item.Points), ct);
            gameFinished = true;
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning(
                "Concurrency conflict capturing item {ItemId} in session {SessionId} by player {PlayerId}",
                item.Id, sessionId, playerId);
            return Result<CaptureResponse>.Conflict("היריב הקדים אותך, הפריט כבר נתפס");
        }

        return Result<CaptureResponse>.Ok(new CaptureResponse
        {
            ItemId = item.Id,
            Points = item.Points,
            GameFinished = gameFinished
        });
    }
}