using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using GameHub.Service;
using Moq;

namespace GameHub.Tests;

public abstract class GameFinisherTestBase
{
    protected const int SessionId = 17;
    protected const int HostId = 1;
    protected const int GuestId = 2;

    protected readonly Mock<IGameItemRepository> Items = new();
    protected readonly Mock<IGameSessionRepository> Sessions = new();
    protected readonly Mock<IUnitOfWork> UnitOfWork = new();
    protected readonly Mock<IClock> Clock = new();
    protected readonly FakeLogger<GameFinisher> Logger = new();
    protected readonly DateTime Now = new(2026, 10, 18, 12, 5, 0, DateTimeKind.Utc);

    protected GameFinisherTestBase()
    {
        Clock.Setup(c => c.UtcNow).Returns(Now);
    }

    protected GameFinisher CreateFinisher()
    {
        return new GameFinisher(
            Items.Object,
            Sessions.Object,
            UnitOfWork.Object,
            Clock.Object,
            Logger);
    }

    protected static GameSession Session()
    {
        var session = new GameSession { Id = SessionId, Status = SessionStatus.InProgress };
        session.Players.Add(new SessionPlayer { SessionId = SessionId, PlayerId = HostId });
        session.Players.Add(new SessionPlayer { SessionId = SessionId, PlayerId = GuestId });
        return session;
    }

    protected void SetupPoints(Dictionary<int, int> points)
    {
        Items.Setup(i => i.GetPointsByPlayerAsync(SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(points);
    }

    protected static SessionPlayer Player(GameSession session, int id) =>
        session.Players.Single(p => p.PlayerId == id);
}