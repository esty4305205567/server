using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using GameHub.Service;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameHub.Tests;

public abstract class GameCaptureServiceTestBase
{
    protected const int SessionId = 17;
    protected const int ItemId = 40;
    protected const int HostId = 1;
    protected const int GuestId = 2;

    protected readonly Mock<IGameSessionRepository> Sessions = new();
    protected readonly Mock<IGameItemRepository> Items = new();
    protected readonly Mock<IUnitOfWork> UnitOfWork = new();
    protected readonly Mock<IGameFinisher> Finisher = new();
    protected readonly Mock<ICaptureRule> Rule = new();
    protected readonly Mock<IClock> Clock = new();
    protected readonly FakeLogger<GameCaptureService> Logger = new();
    protected readonly DateTime Start = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    protected GameCaptureServiceTestBase()
    {
        Clock.Setup(c => c.UtcNow).Returns(Start.AddSeconds(10));
        Rule.Setup(r => r.Type).Returns(GameType.NumberRace);

        Finisher.Setup(f => f.FinishIfExpiredAsync(
                It.IsAny<GameSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameSession session, CancellationToken _) => session);
    }

    protected GameCaptureService CreateService()
    {
        return new GameCaptureService(
            Sessions.Object,
            Items.Object,
            UnitOfWork.Object,
            Finisher.Object,
            new[] { Rule.Object },
            Clock.Object,
            Logger);
    }

    protected GameSession ActiveSession(GameType type = GameType.NumberRace)
    {
        return new GameSession
        {
            Id = SessionId,
            Type = type,
            Status = SessionStatus.InProgress,
            HostId = HostId,
            GuestId = GuestId,
            DurationSeconds = 60,
            StartedAtUtc = Start
        };
    }

    protected GameItem OpenItem()
    {
        return new GameItem
        {
            Id = ItemId,
            SessionId = SessionId,
            Order = 1,
            Points = 3
        };
    }

    protected void SetupSessionAndItem(GameSession session, GameItem item)
    {
        Sessions.Setup(s => s.GetByIdAsync(SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        Items.Setup(i => i.GetByIdInSessionAsync(SessionId, ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
    }
}