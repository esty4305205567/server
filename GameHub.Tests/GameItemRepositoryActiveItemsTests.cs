using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Data.Repositories;

namespace GameHub.Tests;

public class GameItemRepositoryActiveItemsTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;

    public GameItemRepositoryActiveItemsTests(PostgresTestDatabase database)
    {
        _database = database;
    }

    private async Task<(int SessionId, int[] ItemIds, int PlayerId)> SeedAsync(
        string code, GameType type, params (int? Appear, int? Disappear)[] windows)
    {
        await using var seed = _database.CreateContext();

        var host = new Player { Username = code + "_h", PasswordHash = "x", Role = PlayerRole.Player };
        var guest = new Player { Username = code + "_g", PasswordHash = "x", Role = PlayerRole.Player };
        seed.Players.AddRange(host, guest);
        await seed.SaveChangesAsync();

        var session = new GameSession
        {
            Code = code,
            Type = type,
            Status = SessionStatus.InProgress,
            HostId = host.Id,
            GuestId = guest.Id,
            DurationSeconds = 60,
            Speed = 3,
            CreatedAtUtc = DateTime.UtcNow,
            StartedAtUtc = DateTime.UtcNow
        };

        for (var index = 0; index < windows.Length; index++)
        {
            session.Items.Add(new GameItem
            {
                Order = index + 1,
                DisplayText = string.Empty,
                Points = 10,
                AppearAtMs = windows[index].Appear,
                DisappearAtMs = windows[index].Disappear
            });
        }

        seed.GameSessions.Add(session);
        await seed.SaveChangesAsync();

        var itemIds = session.Items.OrderBy(i => i.Order).Select(i => i.Id).ToArray();
        return (session.Id, itemIds, host.Id);
    }

    private async Task CaptureAsync(int itemId, int playerId)
    {
        await using var context = _database.CreateContext();
        var item = await context.GameItems.FindAsync(itemId);
        item!.CapturedByPlayerId = playerId;
        item.CapturedAtUtc = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    private async Task<IReadOnlyList<GameItem>> GetActiveLootAsync(int sessionId, int elapsedMs)
    {
        await using var context = _database.CreateContext();
        return await new GameItemRepository(context)
            .GetActiveLootItemsAsync(sessionId, elapsedMs, CancellationToken.None);
    }

    [Fact]
    public async Task GetActiveLootItemsAsync_ItemInsideWindow_IsReturned()
    {
        var (sessionId, itemIds, _) = await SeedAsync("ACT1", GameType.LootCollection, (0, 3000));

        var items = await GetActiveLootAsync(sessionId, 1000);

        Assert.Equal(itemIds[0], Assert.Single(items).Id);
    }

    [Fact]
    public async Task GetActiveLootItemsAsync_NotYetAppeared_IsNotReturned()
    {
        var (sessionId, _, _) = await SeedAsync("ACT2", GameType.LootCollection, (5000, 8000));

        var items = await GetActiveLootAsync(sessionId, 1000);

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetActiveLootItemsAsync_AlreadyDisappeared_IsNotReturned()
    {
        var (sessionId, _, _) = await SeedAsync("ACT3", GameType.LootCollection, (0, 3000));

        var items = await GetActiveLootAsync(sessionId, 4000);

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetActiveLootItemsAsync_ExactlyAtAppearTime_IsReturned()
    {
        var (sessionId, itemIds, _) = await SeedAsync("ACT4", GameType.LootCollection, (2000, 5000));

        var items = await GetActiveLootAsync(sessionId, 2000);

        Assert.Equal(itemIds[0], Assert.Single(items).Id);
    }

    [Fact]
    public async Task GetActiveLootItemsAsync_ExactlyAtDisappearTime_IsNotReturned()
    {
        var (sessionId, _, _) = await SeedAsync("ACT5", GameType.LootCollection, (0, 3000));

        var items = await GetActiveLootAsync(sessionId, 3000);

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetActiveLootItemsAsync_CapturedItem_IsNotReturned()
    {
        var (sessionId, itemIds, playerId) = await SeedAsync(
            "ACT6", GameType.LootCollection, (0, 3000), (0, 3000));
        await CaptureAsync(itemIds[0], playerId);

        var items = await GetActiveLootAsync(sessionId, 1000);

        Assert.Equal(itemIds[1], Assert.Single(items).Id);
    }

    [Fact]
    public async Task GetActiveLootItemsAsync_ItemWithoutWindow_IsNotReturned()
    {
        var (sessionId, _, _) = await SeedAsync(
            "ACT7", GameType.NumberRace, (null, null), (null, null));

        var items = await GetActiveLootAsync(sessionId, 1000);

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetCurrentRoundItemAsync_ReturnsLowestUncapturedOrder()
    {
        var (sessionId, itemIds, playerId) = await SeedAsync(
            "ACT8", GameType.NumberRace, (null, null), (null, null), (null, null));
        await CaptureAsync(itemIds[0], playerId);

        await using var context = _database.CreateContext();
        var item = await new GameItemRepository(context)
            .GetCurrentRoundItemAsync(sessionId, CancellationToken.None);

        Assert.NotNull(item);
        Assert.Equal(itemIds[1], item!.Id);
        Assert.Equal(2, item.Order);
    }

    [Fact]
    public async Task GetCurrentRoundItemAsync_AllCaptured_ReturnsNull()
    {
        var (sessionId, itemIds, playerId) = await SeedAsync(
            "ACT9", GameType.NumberRace, (null, null), (null, null));
        foreach (var id in itemIds)
        {
            await CaptureAsync(id, playerId);
        }

        await using var context = _database.CreateContext();
        var item = await new GameItemRepository(context)
            .GetCurrentRoundItemAsync(sessionId, CancellationToken.None);

        Assert.Null(item);
    }
}