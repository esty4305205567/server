using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Exceptions;
using GameHub.Core.Results;
using Moq;
namespace GameHub.Tests;

public class GameSessionServiceCreateTests : GameSessionServiceTestBase
{
    private const int HostId = 5;

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsWaitingSession()
    {
        Players.Setup(p => p.GetByIdAsync(HostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Player { Id = HostId, Username = "dana_k" });

        Settings.Setup(s => s.GetAsync(GameType.LootCollection, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GameSetting
            {
                GameType = GameType.LootCollection,
                DurationSeconds = 90,
                Speed = 4
            });

        CodeGenerator.Setup(c => c.Generate()).Returns("K7M2Q");

        Sessions.Setup(s => s.CodeExistsAsync("K7M2Q", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();

        var result = await service.CreateAsync(GameType.LootCollection, HostId, CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal("K7M2Q", result.Value!.Code);
        Assert.Equal(SessionStatus.Waiting, result.Value.Status);
        Assert.Equal(90, result.Value.DurationSeconds);
        Assert.Equal(4, result.Value.Speed);
        Assert.Equal("dana_k", result.Value.HostUsername);

        Sessions.Verify(s => s.AddAsync(
            It.Is<GameSession>(g => g.HostId == HostId && g.Players.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_LootCollection_ConfiguresSessionBeforeSaving()
    {
        Players.Setup(p => p.GetByIdAsync(HostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Player { Id = HostId, Username = "dana_k" });

        Settings.Setup(s => s.GetAsync(GameType.LootCollection, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GameSetting
            {
                GameType = GameType.LootCollection,
                DurationSeconds = 90,
                Speed = 4
            });

        CodeGenerator.Setup(c => c.Generate()).Returns("K7M2Q");

        Sessions.Setup(s => s.CodeExistsAsync("K7M2Q", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var calls = new List<string>();
        GameSession? configured = null;
        GameSession? added = null;

        Configurator.Setup(c => c.Configure(It.IsAny<GameSession>()))
            .Callback<GameSession>(s =>
            {
                configured = s;
                calls.Add("Configure");
            });

        Sessions.Setup(s => s.AddAsync(It.IsAny<GameSession>(), It.IsAny<CancellationToken>()))
            .Callback<GameSession, CancellationToken>((s, _) =>
            {
                added = s;
                calls.Add("AddAsync");
            })
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var result = await service.CreateAsync(GameType.LootCollection, HostId, CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(new[] { "Configure", "AddAsync" }, calls);
        Assert.NotNull(configured);
        Assert.Same(configured, added);
    }

    [Fact]
    public async Task CreateAsync_NumberRace_DoesNotConfigureSession()
    {
        Players.Setup(p => p.GetByIdAsync(HostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Player { Id = HostId, Username = "dana_k" });

        Settings.Setup(s => s.GetAsync(GameType.NumberRace, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GameSetting { GameType = GameType.NumberRace, DurationSeconds = 60, Speed = 3 });

        CodeGenerator.Setup(c => c.Generate()).Returns("K7M2Q");

        Sessions.Setup(s => s.CodeExistsAsync("K7M2Q", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();

        var result = await service.CreateAsync(GameType.NumberRace, HostId, CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);

        Configurator.Verify(c => c.Configure(It.IsAny<GameSession>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_HostNotFound_ReturnsNotFound()
    {
        Players.Setup(p => p.GetByIdAsync(HostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Player?)null);

        var service = CreateService();

        var result = await service.CreateAsync(GameType.LootCollection, HostId, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("המשתמש לא נמצא", result.Message);

        Sessions.Verify(s => s.AddAsync(It.IsAny<GameSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_SettingMissing_ReturnsNotFound()
    {
        Players.Setup(p => p.GetByIdAsync(HostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Player { Id = HostId, Username = "dana_k" });

        Settings.Setup(s => s.GetAsync(GameType.QuickQuiz, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameSetting?)null);

        var service = CreateService();

        var result = await service.CreateAsync(GameType.QuickQuiz, HostId, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("הגדרות המשחק לא נמצאו", result.Message);

        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_FirstCodeTaken_RetriesWithNewCode()
    {
        Players.Setup(p => p.GetByIdAsync(HostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Player { Id = HostId, Username = "dana_k" });

        Settings.Setup(s => s.GetAsync(GameType.NumberRace, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GameSetting { GameType = GameType.NumberRace, DurationSeconds = 60, Speed = 3 });

        CodeGenerator.SetupSequence(c => c.Generate())
            .Returns("AAAAA")
            .Returns("BBBBB");

        Sessions.Setup(s => s.CodeExistsAsync("AAAAA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Sessions.Setup(s => s.CodeExistsAsync("BBBBB", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService();

        var result = await service.CreateAsync(GameType.NumberRace, HostId, CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("BBBBB", result.Value!.Code);

        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_AllCodesTaken_ReturnsConflict()
    {
        Players.Setup(p => p.GetByIdAsync(HostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Player { Id = HostId, Username = "dana_k" });

        Settings.Setup(s => s.GetAsync(GameType.NumberRace, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GameSetting { GameType = GameType.NumberRace, DurationSeconds = 60, Speed = 3 });

        CodeGenerator.Setup(c => c.Generate()).Returns("AAAAA");

        Sessions.Setup(s => s.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = CreateService();

        var result = await service.CreateAsync(GameType.NumberRace, HostId, CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal("לא הצלחנו ליצור קוד למשחק, נסו שוב", result.Message);

        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_CodeCollidesOnSave_DetachesAndRetries()
    {
        Players.Setup(p => p.GetByIdAsync(HostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Player { Id = HostId, Username = "dana_k" });

        Settings.Setup(s => s.GetAsync(GameType.QuickQuiz, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GameSetting { GameType = GameType.QuickQuiz, DurationSeconds = 60, Speed = 3 });

        CodeGenerator.SetupSequence(c => c.Generate())
            .Returns("AAAAA")
            .Returns("BBBBB");

        Sessions.Setup(s => s.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        UnitOfWork.SetupSequence(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateCodeException())
            .Returns(Task.CompletedTask);

        var service = CreateService();

        var result = await service.CreateAsync(GameType.QuickQuiz, HostId, CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("BBBBB", result.Value!.Code);

        Sessions.Verify(s => s.Detach(It.Is<GameSession>(g => g.Code == "AAAAA")), Times.Once);
        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}