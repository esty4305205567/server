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

public class AdminGameSessionServiceTests
{
    private static readonly DateTime Created = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Started = new(2026, 10, 18, 12, 1, 0, DateTimeKind.Utc);

    private readonly Mock<IGameSessionRepository> _sessions = new();

    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<AdminGameSessionMappingProfile>()).CreateMapper();

    private AdminGameSessionService CreateService() => new(_sessions.Object, _mapper);

    private static GameSession CreateSession(
        int id, string host, string? guest, SessionStatus status) => new()
        {
            Id = id,
            Code = $"CODE{id}",
            Type = GameType.LootCollection,
            Status = status,
            DurationSeconds = 60,
            CreatedAtUtc = Created,
            StartedAtUtc = guest is null ? null : Started,
            Host = new Player { Id = 1, Username = host },
            Guest = guest is null ? null : new Player { Id = 2, Username = guest }
        };

    [Fact]
    public async Task GetAllAsync_PassesFiltersToRepository()
    {
        _sessions.Setup(s => s.GetAllAsync(
                GameType.NumberRace, SessionStatus.Finished, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameSession>());
        var query = new AdminGameSessionsQuery
        {
            GameType = GameType.NumberRace,
            Status = SessionStatus.Finished
        };

        var result = await CreateService().GetAllAsync(query, CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        _sessions.Verify(s => s.GetAllAsync(
            GameType.NumberRace, SessionStatus.Finished, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_NoFilters_PassesNullsToRepository()
    {
        _sessions.Setup(s => s.GetAllAsync(
                null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameSession>());

        var result = await CreateService().GetAllAsync(
            new AdminGameSessionsQuery(), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Empty(result.Value!);
        _sessions.Verify(s => s.GetAllAsync(
            null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_SessionWithGuest_MapsHostAndGuestUsernames()
    {
        var session = CreateSession(10, "admin", "demo", SessionStatus.InProgress);
        _sessions.Setup(s => s.GetAllAsync(
                It.IsAny<GameType?>(), It.IsAny<SessionStatus?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameSession> { session });

        var result = await CreateService().GetAllAsync(
            new AdminGameSessionsQuery(), CancellationToken.None);

        var item = Assert.Single(result.Value!);
        Assert.Equal(10, item.Id);
        Assert.Equal("CODE10", item.Code);
        Assert.Equal(SessionStatus.InProgress, item.Status);
        Assert.Equal("admin", item.HostUsername);
        Assert.Equal("demo", item.GuestUsername);
        Assert.Equal(60, item.DurationSeconds);
        Assert.Equal(Created, item.CreatedAtUtc);
        Assert.Equal(Started, item.StartedAtUtc);
        Assert.Null(item.FinishedAtUtc);
    }

    [Fact]
    public async Task GetAllAsync_WaitingSessionWithoutGuest_MapsGuestUsernameToNull()
    {
        var session = CreateSession(11, "demo", null, SessionStatus.Waiting);
        _sessions.Setup(s => s.GetAllAsync(
                It.IsAny<GameType?>(), It.IsAny<SessionStatus?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameSession> { session });

        var result = await CreateService().GetAllAsync(
            new AdminGameSessionsQuery(), CancellationToken.None);

        var item = Assert.Single(result.Value!);
        Assert.Equal("demo", item.HostUsername);
        Assert.Null(item.GuestUsername);
        Assert.Null(item.StartedAtUtc);
    }

    [Fact]
    public async Task GetAllAsync_PreservesRepositoryOrder()
    {
        var sessions = new List<GameSession>
        {
            CreateSession(3, "admin", null, SessionStatus.Waiting),
            CreateSession(2, "admin", null, SessionStatus.Waiting),
            CreateSession(1, "admin", null, SessionStatus.Waiting)
        };
        _sessions.Setup(s => s.GetAllAsync(
                It.IsAny<GameType?>(), It.IsAny<SessionStatus?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessions);

        var result = await CreateService().GetAllAsync(
            new AdminGameSessionsQuery(), CancellationToken.None);

        Assert.Equal(new[] { 3, 2, 1 }, result.Value!.Select(i => i.Id).ToArray());
    }
}