using AutoMapper;
using GameHub.Core.DTOs;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using GameHub.Core.Results;
using Microsoft.Extensions.Logging;

namespace GameHub.Service;

public class GameSettingService : IGameSettingService
{
    private const string SettingNotFoundMessage = "הגדרות המשחק לא נמצאו";
    private const string SpeedRequiredMessage = "במשחק איסוף שלל חובה להגדיר מהירות";

    private readonly IGameSettingRepository _settings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IMapper _mapper;
    private readonly ILogger<GameSettingService> _logger;

    public GameSettingService(
        IGameSettingRepository settings,
        IUnitOfWork unitOfWork,
        IClock clock,
        IMapper mapper,
        ILogger<GameSettingService> logger)
    {
        _settings = settings;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<GameSettingResponse>>> GetAllAsync(CancellationToken ct)
    {
        var settings = await _settings.GetAllAsync(ct);
        var response = _mapper.Map<List<GameSettingResponse>>(settings);

        return Result<IReadOnlyList<GameSettingResponse>>.Ok(response);
    }

    public async Task<Result<GameSettingResponse>> UpdateAsync(
        GameType gameType, UpdateGameSettingRequest request, CancellationToken ct)
    {
        var setting = await _settings.GetForUpdateAsync(gameType, ct);
        if (setting is null)
        {
            return Result<GameSettingResponse>.NotFound(SettingNotFoundMessage);
        }

        var usesSpeed = gameType == GameType.LootCollection;
        if (usesSpeed && request.Speed is null)
        {
            return Result<GameSettingResponse>.BadRequest(SpeedRequiredMessage);
        }

        setting.DurationSeconds = request.DurationSeconds;
        if (usesSpeed)
        {
            setting.Speed = request.Speed!.Value;
        }
        setting.UpdatedAtUtc = _clock.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Game setting for {GameType} updated", gameType);

        return Result<GameSettingResponse>.Ok(_mapper.Map<GameSettingResponse>(setting));
    }
}