using AutoMapper;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using GameHub.Service;
using GameHub.Service.Mapping;
using Moq;

namespace GameHub.Tests;

public abstract class GameStateServiceTestBase
{
    protected const int SessionId = 17;
    protected const int HostId = 1;
    protected const int GuestId = 2;

    protected readonly Mock<IGameSessionRepository> Sessions = new();
    protected readonly Mock<IGameItemRepository> Items = new();
    protected readonly Mock<IPlayerRepository> Players = new();
    protected readonly Mock<IGameFinisher> Finisher = new();
    protected readonly Mock<IClock> Clock = new();

    protected readonly IMapper Mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<GameStateMappingProfile>()).CreateMapper();

    protected readonly DateTime Start = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    protected GameStateServiceTestBase()
    {
        Clock.Setup(c => c.UtcNow).Returns(Start.AddSeconds(10));

        Finisher.Setup(f => f.FinishIfExpiredAsync(
                It.IsAny<GameSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameSession session, CancellationToken _) => session);

        SetupUsernames(new Dictionary<int, string> { [HostId] = "dana1", [GuestId] = "yael1" });
        SetupPoints(new Dictionary<int, int>());

        IReadOnlyList<GameItem> noBoxes = new List<GameItem>();
        Items.Setup(i => i.GetActiveLootItemsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(noBoxes);
        Items.Setup(i => i.GetCurrentRoundItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameItem?)null);
    }

    protected GameStateService CreateService()
    {
        return new GameStateService(
            Sessions.Object,
            Items.Object,
            Players.Object,
            Finisher.Object,
            Mapper,
            Clock.Object);
    }

    protected GameSession Session(
        SessionStatus status = SessionStatus.InProgress,
        GameType type = GameType.NumberRace,
        int? guestId = GuestId)
    {
        var session = new GameSession
        {
            Id = SessionId,
            Code = "K7M2Q",
            Type = type,
            Status = status,
            HostId = HostId,
            GuestId = guestId,
            DurationSeconds = 60,
            StartedAtUtc = status == SessionStatus.Waiting ? null : Start
        };

        session.Players.Add(new SessionPlayer { SessionId = SessionId, PlayerId = HostId });

        if (guestId is not null)
        {
            session.Players.Add(new SessionPlayer { SessionId = SessionId, PlayerId = guestId.Value });
        }

        return session;
    }

    protected void SetupSession(GameSession session)
    {
        Sessions.Setup(s => s.GetByIdAsync(SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
    }

    protected void SetupUsernames(Dictionary<int, string> names)
    {
        IReadOnlyDictionary<int, string> value = names;

        Players.Setup(p => p.GetUsernamesAsync(
                It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(value);
    }

    protected void SetupPoints(Dictionary<int, int> points)
    {
        IReadOnlyDictionary<int, int> value = points;

        Items.Setup(i => i.GetPointsByPlayerAsync(SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(value);
    }
}