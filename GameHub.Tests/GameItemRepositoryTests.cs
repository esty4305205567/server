using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Data.Repositories;

namespace GameHub.Tests;

public class GameItemRepositoryTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;

    public GameItemRepositoryTests(PostgresTestDatabase database)
    {
        _database = database;
    }

    private async Task<(int SessionId, int[] ItemIds, int PlayerId)> SeedAsync(string code)
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
            CreatedAtUtc = DateTime.UtcNow,
            StartedAtUtc = DateTime.UtcNow
        };
        for (var order = 1; order <= 3; order++)
        {
            session.Items.Add(new GameItem
            {
                Order = order,
                DisplayText = order.ToString(),
                CorrectAnswer = order.ToString(),
                Points = 3
            });
        }
        seed.GameSessions.Add(session);
        await seed.SaveChangesAsync();

        return (session.Id, session.Items.OrderBy(i => i.Order).Select(i => i.Id).ToArray(), host.Id);
    }

    private async Task CaptureAsync(int itemId, int playerId)
    {
        await using var context = _database.CreateContext();
        var item = await context.GameItems.FindAsync(itemId);
        item!.CapturedByPlayerId = playerId;
        item.CapturedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetCurrentRoundOrderAsync_ReturnsLowestUncapturedOrder()
    {
        var (sessionId, itemIds, playerId) = await SeedAsync("REPO1");
        await CaptureAsync(itemIds[0], playerId);

        await using var context = _database.CreateContext();
        var order = await new GameItemRepository(context)
            .GetCurrentRoundOrderAsync(sessionId, CancellationToken.None);

        Assert.Equal(2, order);
    }

    [Fact]
    public async Task GetCurrentRoundOrderAsync_AllCaptured_ReturnsNull()
    {
        var (sessionId, itemIds, playerId) = await SeedAsync("REPO2");
        foreach (var id in itemIds)
        {
            await CaptureAsync(id, playerId);
        }

        await using var context = _database.CreateContext();
        var order = await new GameItemRepository(context)
            .GetCurrentRoundOrderAsync(sessionId, CancellationToken.None);

        Assert.Null(order);
    }

    [Fact]
    public async Task AnyOtherUncapturedAsync_OthersRemain_ReturnsTrue()
    {
        var (sessionId, itemIds, _) = await SeedAsync("REPO3");

        await using var context = _database.CreateContext();
        var result = await new GameItemRepository(context)
            .AnyOtherUncapturedAsync(sessionId, itemIds[0], CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task AnyOtherUncapturedAsync_OnlyExcludedRemains_ReturnsFalse()
    {
        var (sessionId, itemIds, playerId) = await SeedAsync("REPO4");
        await CaptureAsync(itemIds[0], playerId);
        await CaptureAsync(itemIds[1], playerId);

        await using var context = _database.CreateContext();
        var result = await new GameItemRepository(context)
            .AnyOtherUncapturedAsync(sessionId, itemIds[2], CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task GetByIdInSessionAsync_ItemOfAnotherSession_ReturnsNull()
    {
        var (_, itemIds, _) = await SeedAsync("REPO5");
        var (otherSessionId, _, _) = await SeedAsync("REPO6");

        await using var context = _database.CreateContext();
        var item = await new GameItemRepository(context)
            .GetByIdInSessionAsync(otherSessionId, itemIds[0], CancellationToken.None);

        Assert.Null(item);
    }
    [Fact]
    public async Task GetPointsByPlayerAsync_SumsPointsOfCapturedItems()
    {
        var (sessionId, itemIds, playerId) = await SeedAsync("REPO7");
        await CaptureAsync(itemIds[0], playerId);
        await CaptureAsync(itemIds[1], playerId);

        await using var context = _database.CreateContext();
        var points = await new GameItemRepository(context)
            .GetPointsByPlayerAsync(sessionId, CancellationToken.None);

        Assert.Single(points);
        Assert.Equal(6, points[playerId]);
    }

    [Fact]
    public async Task GetPointsByPlayerAsync_NothingCaptured_ReturnsEmpty()
    {
        var (sessionId, _, _) = await SeedAsync("REPO8");

        await using var context = _database.CreateContext();
        var points = await new GameItemRepository(context)
            .GetPointsByPlayerAsync(sessionId, CancellationToken.None);

        Assert.Empty(points);
    }
}