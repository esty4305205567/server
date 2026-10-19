using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using GameHub.Data;
using GameHub.Data.Repositories;
using GameHub.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameHub.Tests;

public class GameFinisherIntegrationTests : IClassFixture<PostgresTestDatabase>
{
    private static readonly DateTime Start = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    private readonly PostgresTestDatabase _database;

    public GameFinisherIntegrationTests(PostgresTestDatabase database)
    {
        _database = database;
    }

    private async Task<int> SeedSessionAsync(string code)
    {
        await using var seed = _database.CreateContext();

        var host = new Player { Username = code + "_h", PasswordHash = "x", Role = PlayerRole.Player };
        var guest = new Player { Username = code + "_g", PasswordHash = "x", Role = PlayerRole.Player };
        seed.Players.AddRange(host, guest);
        await seed.SaveChangesAsync();

        var session = new GameSession
        {
            Code = code,
            Type = GameType.NumberRace,
            Status = SessionStatus.InProgress,
            HostId = host.Id,
            GuestId = guest.Id,
            DurationSeconds = 60,
            Speed = 3,
            CreatedAtUtc = Start,
            StartedAtUtc = Start
        };
        session.Players.Add(new SessionPlayer { PlayerId = host.Id });
        session.Players.Add(new SessionPlayer { PlayerId = guest.Id });

        seed.GameSessions.Add(session);
        await seed.SaveChangesAsync();

        return session.Id;
    }

    private static (GameFinisher Finisher, GameSessionRepository Sessions, FakeLogger<GameFinisher> Logger)
        Build(AppDbContext context, DateTime now)
    {
        var clock = new Mock<IClock>();
        clock.Setup(c => c.UtcNow).Returns(now);

        var sessions = new GameSessionRepository(context);
        var logger = new FakeLogger<GameFinisher>();

        var finisher = new GameFinisher(
            new GameItemRepository(context),
            sessions,
            new UnitOfWork(context),
            clock.Object,
            logger);

        return (finisher, sessions, logger);
    }

    private async Task<GameSession> LoadFreshAsync(int sessionId)
    {
        await using var context = _database.CreateContext();
        return await context.GameSessions
            .AsNoTracking()
            .Include(s => s.Players)
            .SingleAsync(s => s.Id == sessionId);
    }

    [Fact]
    public async Task FinishIfExpiredAsync_TimeUp_PersistsFinishedSession()
    {
        var sessionId = await SeedSessionAsync("FIN01");

        await using var context = _database.CreateContext();
        var (finisher, sessions, _) = Build(context, Start.AddSeconds(61));
        var session = await sessions.GetByIdAsync(sessionId, CancellationToken.None);

        var result = await finisher.FinishIfExpiredAsync(session!, CancellationToken.None);

        Assert.Same(session, result);

        var stored = await LoadFreshAsync(sessionId);
        Assert.Equal(SessionStatus.Finished, stored.Status);
        Assert.Equal(Start.AddSeconds(61), stored.FinishedAtUtc);
        Assert.All(stored.Players, p => Assert.False(p.IsWinner));
    }

    [Fact]
    public async Task FinishIfExpiredAsync_TimeNotUp_LeavesSessionInProgress()
    {
        var sessionId = await SeedSessionAsync("FIN02");

        await using var context = _database.CreateContext();
        var (finisher, sessions, _) = Build(context, Start.AddSeconds(30));
        var session = await sessions.GetByIdAsync(sessionId, CancellationToken.None);

        var result = await finisher.FinishIfExpiredAsync(session!, CancellationToken.None);

        Assert.Same(session, result);

        var stored = await LoadFreshAsync(sessionId);
        Assert.Equal(SessionStatus.InProgress, stored.Status);
        Assert.Null(stored.FinishedAtUtc);
    }

    [Fact]
    public async Task FinishIfExpiredAsync_AnotherRequestFinishedFirst_ReloadsFinalStateAndLogsWarning()
    {
        var sessionId = await SeedSessionAsync("FIN03");
        var expiredAt = Start.AddSeconds(61);

        await using var contextA = _database.CreateContext();
        await using var contextB = _database.CreateContext();

        var (finisherA, sessionsA, loggerA) = Build(contextA, expiredAt);
        var (finisherB, sessionsB, _) = Build(contextB, expiredAt);

        var staleSession = await sessionsA.GetByIdAsync(sessionId, CancellationToken.None);
        var winningSession = await sessionsB.GetByIdAsync(sessionId, CancellationToken.None);

        await finisherB.FinishIfExpiredAsync(winningSession!, CancellationToken.None);

        var result = await finisherA.FinishIfExpiredAsync(staleSession!, CancellationToken.None);

        Assert.NotSame(staleSession, result);
        Assert.Equal(SessionStatus.Finished, result.Status);
        Assert.Equal(2, result.Players.Count);
        Assert.Contains(loggerA.Entries, e => e.Level == LogLevel.Warning);
        Assert.DoesNotContain(loggerA.Entries, e => e.Level == LogLevel.Error);
    }
}