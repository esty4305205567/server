using AutoMapper;
using GameHub.Core.DTOs;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using GameHub.Core.Results;
using GameHub.Service;
using GameHub.Service.Mapping;
using Moq;

namespace GameHub.Tests;

public class GameHistoryServiceTests
{
    private const int HostId = 1;
    private const int GuestId = 2;

    private readonly Mock<IGameSessionRepository> _sessions = new();

    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<GameHistoryMappingProfile>()).CreateMapper();

    private GameHistoryService CreateService() => new(_sessions.Object, _mapper);

    private void SetupRepository(params GameSession[] sessions)
    {
        _sessions
            .Setup(s => s.GetHistoryAsync(
                It.IsAny<int?>(), It.IsAny<GameType?>(), It.IsAny<GameHistorySortBy>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);
    }

    private static GameSession FinishedSession(int hostScore, int guestScore, int? winnerId)
    {
        var host = new Player { Id = HostId, Username = "dana_k" };
        var guest = new Player { Id = GuestId, Username = "yael_l" };

        return new GameSession
        {
            Id = 17,
            Type = GameType.NumberRace,
            Status = SessionStatus.Finished,
            HostId = HostId,
            GuestId = GuestId,
            Host = host,
            Guest = guest,
            FinishedAtUtc = new DateTime(2026, 10, 18, 12, 1, 0, DateTimeKind.Utc),
            Players = new List<SessionPlayer>
            {
                new()
                {
                    SessionId = 17, PlayerId = HostId, Player = host,
                    Score = hostScore, IsWinner = winnerId == HostId
                },
                new()
                {
                    SessionId = 17, PlayerId = GuestId, Player = guest,
                    Score = guestScore, IsWinner = winnerId == GuestId
                }
            }
        };
    }

    [Fact]
    public async Task GetHistoryAsync_Admin_RequestsAllGames()
    {
        SetupRepository();

        await CreateService().GetHistoryAsync(
            HostId, isAdmin: true, new GameHistoryQuery(), CancellationToken.None);

        _sessions.Verify(s => s.GetHistoryAsync(
            null, It.IsAny<GameType?>(), It.IsAny<GameHistorySortBy>(),
            It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHistoryAsync_Player_RequestsOnlyOwnGames()
    {
        SetupRepository();

        await CreateService().GetHistoryAsync(
            GuestId, isAdmin: false, new GameHistoryQuery(), CancellationToken.None);

        _sessions.Verify(s => s.GetHistoryAsync(
            GuestId, It.IsAny<GameType?>(), It.IsAny<GameHistorySortBy>(),
            It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHistoryAsync_PassesFilterAndSortingToRepository()
    {
        SetupRepository();
        var query = new GameHistoryQuery
        {
            GameType = GameType.QuickQuiz,
            SortBy = GameHistorySortBy.Score,
            Desc = false
        };

        await CreateService().GetHistoryAsync(
            HostId, isAdmin: false, query, CancellationToken.None);

        _sessions.Verify(s => s.GetHistoryAsync(
            HostId, GameType.QuickQuiz, GameHistorySortBy.Score,
            false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetHistoryAsync_MapsScoresAndWinner()
    {
        SetupRepository(FinishedSession(hostScore: 9, guestScore: 12, winnerId: GuestId));

        var result = await CreateService().GetHistoryAsync(
            HostId, isAdmin: false, new GameHistoryQuery(), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        var item = Assert.Single(result.Value!);
        Assert.Equal(17, item.SessionId);
        Assert.Equal(GameType.NumberRace, item.GameType);
        Assert.Equal("dana_k", item.HostUsername);
        Assert.Equal(9, item.HostScore);
        Assert.Equal("yael_l", item.GuestUsername);
        Assert.Equal(12, item.GuestScore);
        Assert.Equal("yael_l", item.WinnerUsername);
    }

    [Fact]
    public async Task GetHistoryAsync_Tie_HasNoWinner()
    {
        SetupRepository(FinishedSession(hostScore: 6, guestScore: 6, winnerId: null));

        var result = await CreateService().GetHistoryAsync(
            HostId, isAdmin: false, new GameHistoryQuery(), CancellationToken.None);

        var item = Assert.Single(result.Value!);
        Assert.Null(item.WinnerUsername);
    }

    [Fact]
    public async Task GetHistoryAsync_NoGames_ReturnsEmptyList()
    {
        SetupRepository();

        var result = await CreateService().GetHistoryAsync(
            HostId, isAdmin: false, new GameHistoryQuery(), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Empty(result.Value!);
    }
}