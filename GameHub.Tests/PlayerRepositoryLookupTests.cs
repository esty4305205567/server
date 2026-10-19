using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Data;
using GameHub.Data.Repositories;

namespace GameHub.Tests;

public class PlayerRepositoryLookupTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;

    public PlayerRepositoryLookupTests(PostgresTestDatabase database)
    {
        _database = database;
    }

    private static Player NewPlayer(string username) =>
        new() { Username = username, PasswordHash = "hash", Role = PlayerRole.Player };

    private async Task SeedAsync(string username)
    {
        await using var seed = _database.CreateContext();
        seed.Players.Add(NewPlayer(username));
        await seed.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByUsernameAsync_ExistingUser_ReturnsPlayer()
    {
        await SeedAsync("byname_found");

        await using var context = _database.CreateContext();
        var player = await new PlayerRepository(context)
            .GetByUsernameAsync("byname_found", CancellationToken.None);

        Assert.NotNull(player);
        Assert.Equal("byname_found", player!.Username);
        Assert.Equal("hash", player.PasswordHash);
        Assert.Equal(PlayerRole.Player, player.Role);
    }

    [Fact]
    public async Task GetByUsernameAsync_UnknownUser_ReturnsNull()
    {
        await using var context = _database.CreateContext();
        var player = await new PlayerRepository(context)
            .GetByUsernameAsync("byname_missing", CancellationToken.None);

        Assert.Null(player);
    }

    [Fact]
    public async Task AddAsync_ThenSave_PersistsPlayerWithGeneratedId()
    {
        await using (var context = _database.CreateContext())
        {
            var player = NewPlayer("byname_added");
            await new PlayerRepository(context).AddAsync(player, CancellationToken.None);
            await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);

            Assert.True(player.Id > 0);
        }

        await using var verify = _database.CreateContext();
        var stored = await new PlayerRepository(verify)
            .GetByUsernameAsync("byname_added", CancellationToken.None);

        Assert.NotNull(stored);
    }

    [Fact]
    public async Task AddAsync_WithoutSave_DoesNotPersist()
    {
        await using (var context = _database.CreateContext())
        {
            await new PlayerRepository(context)
                .AddAsync(NewPlayer("byname_unsaved"), CancellationToken.None);
        }

        await using var verify = _database.CreateContext();
        var stored = await new PlayerRepository(verify)
            .GetByUsernameAsync("byname_unsaved", CancellationToken.None);

        Assert.Null(stored);
    }
}