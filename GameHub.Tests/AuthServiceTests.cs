using AutoMapper;
using GameHub.Core.DTOs;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Exceptions;
using GameHub.Core.Interfaces;
using GameHub.Core.Results;
using GameHub.Service;
using GameHub.Service.Mapping;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameHub.Tests;

public class AuthServiceTests
{
    private const string InvalidCredentials = "שם המשתמש או הסיסמה שגויים";
    private const string UsernameTaken = "שם המשתמש כבר תפוס";

    private readonly Mock<IPlayerRepository> _players = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenService> _tokens = new();
    private readonly FakeLogger<AuthService> _logger = new();

    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<PlayerMappingProfile>()).CreateMapper();

    public AuthServiceTests()
    {
        _hasher.Setup(h => h.Hash("Secret123")).Returns("hashed");
        _hasher.Setup(h => h.DummyHash).Returns("dummy-hash");
    }

    private AuthService CreateService() => new(
        _players.Object,
        _unitOfWork.Object,
        _hasher.Object,
        _tokens.Object,
        _mapper,
        _logger);

    private static Player StoredPlayer(PlayerRole role = PlayerRole.Player) => new()
    {
        Id = 7,
        Username = "dana1",
        PasswordHash = "stored",
        Role = role
    };

    private void SetupLookup(string username, Player? player)
    {
        _players.Setup(p => p.GetByUsernameAsync(username, It.IsAny<CancellationToken>()))
            .ReturnsAsync(player);
    }

    private static RegisterRequest Register() => new() { Username = "dana1", Password = "Secret123" };

    private static LoginRequest Login(string username = "dana1", string password = "Secret123") =>
        new() { Username = username, Password = password };

    [Fact]
    public async Task RegisterAsync_NewUsername_CreatesPlayerWithPlayerRoleAndHashedPassword()
    {
        SetupLookup("dana1", null);
        Player? added = null;
        _players.Setup(p => p.AddAsync(It.IsAny<Player>(), It.IsAny<CancellationToken>()))
            .Callback<Player, CancellationToken>((player, _) => added = player)
            .Returns(Task.CompletedTask);

        var result = await CreateService().RegisterAsync(Register(), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("dana1", result.Value!.Username);
        Assert.Equal(PlayerRole.Player, result.Value.Role);

        Assert.NotNull(added);
        Assert.Equal("hashed", added!.PasswordHash);
        Assert.Equal(PlayerRole.Player, added.Role);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_UsernameAlreadyExists_ReturnsConflictWithoutSaving()
    {
        SetupLookup("dana1", StoredPlayer());

        var result = await CreateService().RegisterAsync(Register(), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal(UsernameTaken, result.Message);
        _players.Verify(p => p.AddAsync(It.IsAny<Player>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_UsernameTakenConcurrentlyOnSave_ReturnsConflictAndLogsWarning()
    {
        SetupLookup("dana1", null);
        _players.Setup(p => p.AddAsync(It.IsAny<Player>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateUsernameException());

        var result = await CreateService().RegisterAsync(Register(), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal(UsernameTaken, result.Message);
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.DoesNotContain(_logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task LoginAsync_CorrectCredentials_ReturnsTokenUsernameAndRole()
    {
        var stored = StoredPlayer();
        SetupLookup("dana1", stored);
        _hasher.Setup(h => h.Verify("Secret123", "stored")).Returns(true);
        _tokens.Setup(t => t.CreateToken(stored)).Returns("jwt-token");

        var result = await CreateService().LoginAsync(Login(), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("jwt-token", result.Value!.Token);
        Assert.Equal("dana1", result.Value.Username);
        Assert.Equal(PlayerRole.Player, result.Value.Role);
    }

    [Fact]
    public async Task LoginAsync_AdminPlayer_ReturnsAdminRole()
    {
        var stored = StoredPlayer(PlayerRole.Admin);
        SetupLookup("dana1", stored);
        _hasher.Setup(h => h.Verify("Secret123", "stored")).Returns(true);
        _tokens.Setup(t => t.CreateToken(stored)).Returns("jwt-token");

        var result = await CreateService().LoginAsync(Login(), CancellationToken.None);

        Assert.Equal(PlayerRole.Admin, result.Value!.Role);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsUnauthorizedWithoutTokenAndLogsWarning()
    {
        SetupLookup("dana1", StoredPlayer());
        _hasher.Setup(h => h.Verify("wrong", "stored")).Returns(false);

        var result = await CreateService().LoginAsync(Login(password: "wrong"), CancellationToken.None);

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        Assert.Equal(InvalidCredentials, result.Message);
        _tokens.Verify(t => t.CreateToken(It.IsAny<Player>()), Times.Never);
        Assert.Contains(_logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task LoginAsync_UnknownUsername_ReturnsUnauthorizedAndStillVerifiesAgainstDummyHash()
    {
        SetupLookup("ghost", null);

        var result = await CreateService().LoginAsync(Login("ghost"), CancellationToken.None);

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        _hasher.Verify(h => h.Verify("Secret123", "dummy-hash"), Times.Once);
        _tokens.Verify(t => t.CreateToken(It.IsAny<Player>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_UnknownUserAndWrongPassword_ReturnTheSameMessage()
    {
        SetupLookup("ghost", null);
        SetupLookup("dana1", StoredPlayer());
        _hasher.Setup(h => h.Verify("wrong", "stored")).Returns(false);

        var unknown = await CreateService().LoginAsync(Login("ghost"), CancellationToken.None);
        var wrongPassword = await CreateService().LoginAsync(Login(password: "wrong"), CancellationToken.None);

        Assert.Equal(unknown.Message, wrongPassword.Message);
    }

    [Fact]
    public async Task LoginAsync_UsernameWithSurroundingSpaces_IsTrimmedBeforeLookup()
    {
        var stored = StoredPlayer();
        SetupLookup("dana1", stored);
        _hasher.Setup(h => h.Verify("Secret123", "stored")).Returns(true);
        _tokens.Setup(t => t.CreateToken(stored)).Returns("jwt-token");

        var result = await CreateService().LoginAsync(Login("  dana1  "), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        _players.Verify(p => p.GetByUsernameAsync("dana1", It.IsAny<CancellationToken>()), Times.Once);
    }
}