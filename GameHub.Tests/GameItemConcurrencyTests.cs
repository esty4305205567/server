using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Exceptions;
using GameHub.Data;
using GameHub.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Tests;

public class GameItemConcurrencyTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;

    public GameItemConcurrencyTests(PostgresTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task TwoContexts_CaptureSameItem_FirstSucceedsSecondGetsConflict()
    {
        int sessionId;
        int itemId;
        int firstPlayerId;
        int secondPlayerId;

        await using (var seed = _database.CreateContext())
        {
            var host = new Player { Username = "item_host", PasswordHash = "x", Role = PlayerRole.Player };
            var guest = new Player { Username = "item_guest", PasswordHash = "x", Role = PlayerRole.Player };
            seed.Players.AddRange(host, guest);
            await seed.SaveChangesAsync();

            var session = new GameSession
            {
                Code = "ITEM1",
                Type = GameType.NumberRace,
                Status = SessionStatus.InProgress,
                HostId = host.Id,
                GuestId = guest.Id,
                DurationSeconds = 60,
                Speed = 3,
                CreatedAtUtc = DateTime.UtcNow,
                StartedAtUtc = DateTime.UtcNow
            };
            session.Items.Add(new GameItem { Order = 1, DisplayText = "1", CorrectAnswer = "1", Points = 3 });
            seed.GameSessions.Add(session);
            await seed.SaveChangesAsync();

            sessionId = session.Id;
            itemId = session.Items.Single().Id;
            firstPlayerId = host.Id;
            secondPlayerId = guest.Id;
        }

        await using var context1 = _database.CreateContext();
        await using var context2 = _database.CreateContext();
        var repository1 = new GameItemRepository(context1);
        var repository2 = new GameItemRepository(context2);
        var unitOfWork1 = new UnitOfWork(context1);
        var unitOfWork2 = new UnitOfWork(context2);

        var item1 = await repository1.GetByIdInSessionAsync(sessionId, itemId, CancellationToken.None);
        var item2 = await repository2.GetByIdInSessionAsync(sessionId, itemId, CancellationToken.None);

        Assert.NotNull(item1);
        Assert.NotNull(item2);
        Assert.Null(item1!.CapturedByPlayerId);
        Assert.Null(item2!.CapturedByPlayerId);

        item1.CapturedByPlayerId = firstPlayerId;
        item1.CapturedAtUtc = DateTime.UtcNow;
        item2.CapturedByPlayerId = secondPlayerId;
        item2.CapturedAtUtc = DateTime.UtcNow;

        await unitOfWork1.SaveChangesAsync(CancellationToken.None);

        await Assert.ThrowsAsync<ConcurrencyConflictException>(
            () => unitOfWork2.SaveChangesAsync(CancellationToken.None));

        await using var verify = _database.CreateContext();
        var saved = await verify.GameItems.AsNoTracking().SingleAsync(i => i.Id == itemId);

        Assert.Equal(firstPlayerId, saved.CapturedByPlayerId);
    }
}