using AutoMapper;
using GameHub.Core.DTOs;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Exceptions;
using GameHub.Core.Interfaces;
using GameHub.Core.Results;
using Microsoft.Extensions.Logging;

namespace GameHub.Service;

public class GameSessionService : IGameSessionService
{
    private const int MaxCodeAttempts = 3;

    private readonly IGameSessionRepository _sessions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGameSettingRepository _settings;
    private readonly IPlayerRepository _players;
    private readonly IGameCodeGenerator _codeGenerator;
    private readonly IClock _clock;
    private readonly IMapper _mapper;
    private readonly IReadOnlyCollection<IGameItemGenerator> _itemGenerators;
    private readonly IReadOnlyCollection<IGameSessionConfigurator> _configurators;
    private readonly ILogger<GameSessionService> _logger;

    public GameSessionService(
        IGameSessionRepository sessions,
        IUnitOfWork unitOfWork,
        IGameSettingRepository settings,
        IPlayerRepository players,
        IGameCodeGenerator codeGenerator,
        IEnumerable<IGameItemGenerator> itemGenerators,
        IEnumerable<IGameSessionConfigurator> configurators,
        IMapper mapper,
        IClock clock,
        ILogger<GameSessionService> logger)
    {
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _settings = settings;
        _players = players;
        _codeGenerator = codeGenerator;
        _itemGenerators = itemGenerators.ToList();
        _configurators = configurators.ToList();
        _mapper = mapper;
        _clock = clock;
        _logger = logger;
    }

    public async Task<Result<GameSessionResponse>> CreateAsync(
        GameType gameType, int hostId, CancellationToken ct)
    {
        var host = await _players.GetByIdAsync(hostId, ct);
        if (host is null)
        {
            return Result<GameSessionResponse>.NotFound("המשתמש לא נמצא");
        }

        var setting = await _settings.GetAsync(gameType, ct);
        if (setting is null)
        {
            _logger.LogError("GameSetting missing for game type {GameType}", gameType);
            return Result<GameSessionResponse>.NotFound("הגדרות המשחק לא נמצאו");
        }

        for (var attempt = 1; attempt <= MaxCodeAttempts; attempt++)
        {
            var code = _codeGenerator.Generate();

            if (await _sessions.CodeExistsAsync(code, ct))
            {
                continue;
            }

            var session = new GameSession
            {
                Code = code,
                Type = gameType,
                Status = SessionStatus.Waiting,
                HostId = hostId,
                DurationSeconds = setting.DurationSeconds,
                Speed = setting.Speed,
                CreatedAtUtc = _clock.UtcNow
            };

            session.Players.Add(new SessionPlayer { PlayerId = hostId });

            _configurators.FirstOrDefault(c => c.Type == gameType)?.Configure(session);

            await _sessions.AddAsync(session, ct);

            try
            {
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (DuplicateCodeException)
            {
                _logger.LogWarning(
                    "Game code {Code} collided on save, attempt {Attempt}", code, attempt);
                _sessions.Detach(session);
                continue;
            }

            var response = _mapper.Map<GameSessionResponse>(
                new HostedSession(session, host.Username));

            return Result<GameSessionResponse>.Ok(response);
        }

        _logger.LogError(
            "Failed to generate a unique game code after {Attempts} attempts", MaxCodeAttempts);
        return Result<GameSessionResponse>.Conflict("לא הצלחנו ליצור קוד למשחק, נסו שוב");
    }

    public async Task<Result<JoinGameSessionResponse>> JoinAsync(
        string code, int playerId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result<JoinGameSessionResponse>.BadRequest("יש להזין קוד משחק");
        }

        var player = await _players.GetByIdAsync(playerId, ct);
        if (player is null)
        {
            return Result<JoinGameSessionResponse>.NotFound("המשתמש לא נמצא");
        }

        var session = await _sessions.GetByCodeAsync(code.Trim().ToUpperInvariant(), ct);
        if (session is null)
        {
            return Result<JoinGameSessionResponse>.NotFound("קוד המשחק לא נמצא");
        }

        if (session.HostId == playerId)
        {
            return Result<JoinGameSessionResponse>.BadRequest("אי אפשר להצטרף למשחק שפתחת בעצמך");
        }

        if (session.Status != SessionStatus.Waiting || session.GuestId is not null)
        {
            return Result<JoinGameSessionResponse>.Conflict("המשחק כבר מלא");
        }

        var generator = _itemGenerators.FirstOrDefault(g => g.Type == session.Type);
        if (generator is null)
        {
            _logger.LogError("No item generator registered for game type {GameType}", session.Type);
            return Result<JoinGameSessionResponse>.NotFound("סוג המשחק אינו נתמך");
        }

        var now = _clock.UtcNow;

        session.GuestId = playerId;
        session.Status = SessionStatus.InProgress;
        session.StartedAtUtc = now;
        session.Players.Add(new SessionPlayer { SessionId = session.Id, PlayerId = playerId });

        foreach (var item in generator.Generate(session))
        {
            session.Items.Add(item);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning(
                "Concurrency conflict joining session {SessionId} with code {Code} by player {PlayerId}",
                session.Id, session.Code, playerId);
            return Result<JoinGameSessionResponse>.Conflict("היריב הקדים אותך, המשחק כבר מלא");
        }

        var response = _mapper.Map<JoinGameSessionResponse>(
            new HostedSession(session, session.Host.Username));

        return Result<JoinGameSessionResponse>.Ok(response);
    }
}