using AutoMapper;
using GameHub.Core.DTOs;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using GameHub.Core.Results;
using GameHub.Service;
using GameHub.Service.Mapping;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GameHub.Tests;

public class GameSettingServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime OldUpdate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IGameSettingRepository> _settings = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clock = new();

    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<GameSettingMappingProfile>()).CreateMapper();

    public GameSettingServiceTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
    }

    private GameSettingService CreateService() => new(
        _settings.Object,
        _unitOfWork.Object,
        _clock.Object,
        _mapper,
        NullLogger<GameSettingService>.Instance);

    private static GameSetting CreateSetting(GameType type, int duration = 60, int speed = 3) => new()
    {
        GameType = type,
        DurationSeconds = duration,
        Speed = speed,
        UpdatedAtUtc = OldUpdate
    };

    private GameSetting SetupExisting(GameType type, int duration = 60, int speed = 3)
    {
        var setting = CreateSetting(type, duration, speed);
        _settings.Setup(s => s.GetForUpdateAsync(type, It.IsAny<CancellationToken>()))
            .ReturnsAsync(setting);
        return setting;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllSettingsMapped()
    {
        var all = new List<GameSetting>
        {
            CreateSetting(GameType.LootCollection, 90, 4),
            CreateSetting(GameType.NumberRace, 120, 1),
            CreateSetting(GameType.QuickQuiz, 150, 2)
        };
        _settings.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(all);

        var result = await CreateService().GetAllAsync(CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(3, result.Value!.Count);
        Assert.Equal(GameType.LootCollection, result.Value[0].GameType);
        Assert.Equal(90, result.Value[0].DurationSeconds);
        Assert.Equal(4, result.Value[0].Speed);
        Assert.Equal(OldUpdate, result.Value[0].UpdatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_LootCollectionWithSpeed_UpdatesDurationSpeedAndTimestampAndSavesOnce()
    {
        var setting = SetupExisting(GameType.LootCollection);
        var request = new UpdateGameSettingRequest { DurationSeconds = 120, Speed = 5 };

        var result = await CreateService().UpdateAsync(
            GameType.LootCollection, request, CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(120, setting.DurationSeconds);
        Assert.Equal(5, setting.Speed);
        Assert.Equal(Now, setting.UpdatedAtUtc);
        Assert.Equal(120, result.Value!.DurationSeconds);
        Assert.Equal(5, result.Value.Speed);
        Assert.Equal(Now, result.Value.UpdatedAtUtc);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_LootCollectionWithoutSpeed_ReturnsBadRequestAndDoesNotSave()
    {
        var setting = SetupExisting(GameType.LootCollection);
        var request = new UpdateGameSettingRequest { DurationSeconds = 120, Speed = null };

        var result = await CreateService().UpdateAsync(
            GameType.LootCollection, request, CancellationToken.None);

        Assert.Equal(ResultStatus.BadRequest, result.Status);
        Assert.Equal("במשחק איסוף שלל חובה להגדיר מהירות", result.Message);
        Assert.Equal(60, setting.DurationSeconds);
        Assert.Equal(OldUpdate, setting.UpdatedAtUtc);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(GameType.NumberRace)]
    [InlineData(GameType.QuickQuiz)]
    public async Task UpdateAsync_RoundBasedGameWithSpeed_UpdatesDurationAndIgnoresSpeed(GameType type)
    {
        var setting = SetupExisting(type, duration: 60, speed: 3);
        var request = new UpdateGameSettingRequest { DurationSeconds = 200, Speed = 5 };

        var result = await CreateService().UpdateAsync(type, request, CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(200, setting.DurationSeconds);
        Assert.Equal(3, setting.Speed);
        Assert.Equal(Now, setting.UpdatedAtUtc);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(GameType.NumberRace)]
    [InlineData(GameType.QuickQuiz)]
    public async Task UpdateAsync_RoundBasedGameWithoutSpeed_Succeeds(GameType type)
    {
        var setting = SetupExisting(type, duration: 60, speed: 3);
        var request = new UpdateGameSettingRequest { DurationSeconds = 200, Speed = null };

        var result = await CreateService().UpdateAsync(type, request, CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(200, setting.DurationSeconds);
        Assert.Equal(3, setting.Speed);
    }

    [Fact]
    public async Task UpdateAsync_SettingDoesNotExist_ReturnsNotFoundAndDoesNotSave()
    {
        _settings.Setup(s => s.GetForUpdateAsync(GameType.QuickQuiz, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameSetting?)null);
        var request = new UpdateGameSettingRequest { DurationSeconds = 120, Speed = 3 };

        var result = await CreateService().UpdateAsync(
            GameType.QuickQuiz, request, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("הגדרות המשחק לא נמצאו", result.Message);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}