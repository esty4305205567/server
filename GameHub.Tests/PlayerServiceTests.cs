using AutoMapper;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using GameHub.Core.Results;
using GameHub.Service;
using GameHub.Service.Mapping;
using Moq;

namespace GameHub.Tests;

public class PlayerServiceTests
{
    private const int CurrentId = 7;
    private const int OtherId = 8;

    private readonly Mock<IPlayerRepository> _players = new();

    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<PlayerMappingProfile>()).CreateMapper();

    private PlayerService CreateService() => new(_players.Object, _mapper);

    private void SetupPlayer(int id, string username, PlayerRole role = PlayerRole.Player)
    {
        _players.Setup(p => p.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Player
            {
                Id = id,
                Username = username,
                PasswordHash = "secret-hash",
                Role = role
            });
    }

    private void SetupMissing(int id)
    {
        _players.Setup(p => p.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Player?)null);
    }

    [Fact]
    public async Task GetByIdAsync_OwnAccount_ReturnsIdUsernameAndRole()
    {
        SetupPlayer(CurrentId, "dana1");

        var result = await CreateService().GetByIdAsync(CurrentId, CurrentId, false, CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(CurrentId, result.Value!.Id);
        Assert.Equal("dana1", result.Value.Username);
        Assert.Equal(PlayerRole.Player, result.Value.Role);
    }

    [Fact]
    public async Task GetByIdAsync_AnotherAccountAsRegularPlayer_ReturnsForbiddenWithoutQueryingTheRepository()
    {
        SetupPlayer(OtherId, "yael1");

        var result = await CreateService().GetByIdAsync(OtherId, CurrentId, false, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal("אין לך הרשאה לצפות בחשבון הזה", result.Message);
        _players.Verify(p => p.GetByIdAsync(
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingAccountAsRegularPlayer_ReturnsTheSameForbidden()
    {
        SetupMissing(999);

        var result = await CreateService().GetByIdAsync(999, CurrentId, false, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        _players.Verify(p => p.GetByIdAsync(
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_AnotherAccountAsAdmin_ReturnsTheAccount()
    {
        SetupPlayer(OtherId, "yael1");

        var result = await CreateService().GetByIdAsync(OtherId, CurrentId, true, CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal("yael1", result.Value!.Username);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingAccountAsAdmin_ReturnsNotFound()
    {
        SetupMissing(999);

        var result = await CreateService().GetByIdAsync(999, CurrentId, true, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("השחקן לא נמצא", result.Message);
    }

    [Fact]
    public async Task GetByIdAsync_OwnAccountThatNoLongerExists_ReturnsNotFound()
    {
        SetupMissing(CurrentId);

        var result = await CreateService().GetByIdAsync(CurrentId, CurrentId, false, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}