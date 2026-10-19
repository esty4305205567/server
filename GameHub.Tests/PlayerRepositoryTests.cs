using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Data.Repositories;

namespace GameHub.Tests;

public class PlayerRepositoryTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;

    public PlayerRepositoryTests(PostgresTestDatabase database)
    {
        _database = database;
    }

    private async Task<int[]> SeedPlayersAsync(string prefix, int count)
    {
        await using var seed = _database.CreateContext();

        var players = Enumerable.Range(1, count)
            .Select(n => new Player
            {
                Username = $"{prefix}_{n}",
                PasswordHash = "x",
                Role = PlayerRole.Player
            })
            .ToList();

        seed.Players.AddRange(players);
        await seed.SaveChangesAsync();

        return players.Select(p => p.Id).ToArray();
    }

    [Fact]
    public async Task GetUsernamesAsync_ReturnsOnlyRequestedPlayers()
    {
        var ids = await SeedPlayersAsync("usr_a", 3);

        await using var context = _database.CreateContext();
        var names = await new PlayerRepository(context)
            .GetUsernamesAsync(new[] { ids[0], ids[2] }, CancellationToken.None);

        Assert.Equal(2, names.Count);
        Assert.Equal("usr_a_1", names[ids[0]]);
        Assert.Equal("usr_a_3", names[ids[2]]);
        Assert.False(names.ContainsKey(ids[1]));
    }

    [Fact]
    public async Task GetUsernamesAsync_UnknownId_IsOmitted()
    {
        var ids = await SeedPlayersAsync("usr_b", 1);

        await using var context = _database.CreateContext();
        var names = await new PlayerRepository(context)
            .GetUsernamesAsync(new[] { ids[0], int.MaxValue }, CancellationToken.None);

        Assert.Single(names);
        Assert.Equal("usr_b_1", names[ids[0]]);
    }

    [Fact]
    public async Task GetUsernamesAsync_NoIds_ReturnsEmpty()
    {
        await using var context = _database.CreateContext();
        var names = await new PlayerRepository(context)
            .GetUsernamesAsync(Array.Empty<int>(), CancellationToken.None);

        Assert.Empty(names);
    }
}