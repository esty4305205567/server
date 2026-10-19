using AutoMapper;
using GameHub.Core.DTOs;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Service;
using GameHub.Service.Mapping;

namespace GameHub.Tests;

public class GameSessionMappingProfileTests
{
    private static readonly DateTime StartedAt =
        new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    private readonly IMapper _mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<GameSessionMappingProfile>()).CreateMapper();

    private static GameSession CreateSession()
    {
        return new GameSession
        {
            Id = 17,
            Code = "K7M2Q",
            Type = GameType.NumberRace,
            Status = SessionStatus.InProgress,
            DurationSeconds = 60,
            Speed = 3,
            StartedAtUtc = StartedAt
        };
    }

    [Fact]
    public void Configuration_IsValid()
    {
        var configuration = new MapperConfiguration(
            cfg => cfg.AddProfile<GameSessionMappingProfile>());

        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_HostedSessionToGameSessionResponse_UsesHostUsernameFromRecord()
    {
        var session = CreateSession();

        var response = _mapper.Map<GameSessionResponse>(new HostedSession(session, "dana_k"));

        Assert.Equal(17, response.Id);
        Assert.Equal("K7M2Q", response.Code);
        Assert.Equal(GameType.NumberRace, response.Type);
        Assert.Equal(SessionStatus.InProgress, response.Status);
        Assert.Equal(60, response.DurationSeconds);
        Assert.Equal(3, response.Speed);
        Assert.Equal("dana_k", response.HostUsername);
    }

    [Fact]
    public void Map_HostedSessionToJoinResponse_MapsStartedAtAndHostUsername()
    {
        var session = CreateSession();

        var response = _mapper.Map<JoinGameSessionResponse>(new HostedSession(session, "dana_k"));

        Assert.Equal(17, response.Id);
        Assert.Equal("dana_k", response.HostUsername);
        Assert.Equal(StartedAt, response.StartedAtUtc);
    }

    [Fact]
    public void Map_HostedSessionToJoinResponse_WithoutStartedAt_Throws()
    {
        var session = CreateSession();
        session.StartedAtUtc = null;

        var exception = Assert.Throws<AutoMapperMappingException>(
            () => _mapper.Map<JoinGameSessionResponse>(new HostedSession(session, "dana_k")));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }
}