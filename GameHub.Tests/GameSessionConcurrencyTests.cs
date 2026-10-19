using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Exceptions;
using GameHub.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using GameHub.Data;
namespace GameHub.Tests;

public class GameSessionConcurrencyTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;

    public GameSessionConcurrencyTests(PostgresTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task TwoContexts_JoinSameSession_FirstSucceedsSecondGetsConflict()
    {
        const string code = "K7M2Q";
        int firstGuestId;
        int secondGuestId;

        await using (var seed = _database.CreateContext())
        {
            var host = new Player { Username = "host", PasswordHash = "x", Role = PlayerRole.Player };
            var first = new Player { Username = "first", PasswordHash = "x", Role = PlayerRole.Player };
            var second = new Player { Username = "second", PasswordHash = "x", Role = PlayerRole.Player };
            seed.Players.AddRange(host, first, second);
            await seed.SaveChangesAsync();

            seed.GameSessions.Add(new GameSession
            {
                Code = code,
                Type = GameType.NumberRace,
                Status = SessionStatus.Waiting,
                HostId = host.Id,
                DurationSeconds = 60,
                Speed = 3,
                CreatedAtUtc = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();

            firstGuestId = first.Id;
            secondGuestId = second.Id;
        }

        await using var context1 = _database.CreateContext();
        await using var context2 = _database.CreateContext();
        var repository1 = new GameSessionRepository(context1);
        var repository2 = new GameSessionRepository(context2);
        var unitOfWork1 = new UnitOfWork(context1);
        var unitOfWork2 = new UnitOfWork(context2);
        var session1 = await repository1.GetByCodeAsync(code, CancellationToken.None);
        var session2 = await repository2.GetByCodeAsync(code, CancellationToken.None);

        Assert.NotNull(session1);
        Assert.NotNull(session2);
        Assert.Null(session1!.GuestId);
        Assert.Null(session2!.GuestId);

        session1.GuestId = firstGuestId;
        session1.Status = SessionStatus.InProgress;
        session2.GuestId = secondGuestId;
        session2.Status = SessionStatus.InProgress;

        await unitOfWork1.SaveChangesAsync(CancellationToken.None);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => unitOfWork2.SaveChangesAsync(CancellationToken.None));
        await using var verify = _database.CreateContext();
        var saved = await verify.GameSessions.AsNoTracking().SingleAsync(s => s.Code == code);

        Assert.Equal(firstGuestId, saved.GuestId);
        Assert.Equal(SessionStatus.InProgress, saved.Status);
    }
}