using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Exceptions;
using GameHub.Data;
using GameHub.Data.Repositories;

namespace GameHub.Tests;

public class PlayerRepositoryCaseInsensitivityTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;

    public PlayerRepositoryCaseInsensitivityTests(PostgresTestDatabase database)
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

    [Theory]
    [InlineData("ci_find_a1", "ci_find_a1")]
    [InlineData("ci_find_a2", "CI_FIND_A2")]
    [InlineData("ci_find_a3", "Ci_Find_A3")]
    public async Task GetByUsernameAsync_AnyCasing_FindsTheSamePlayer(string stored, string lookup)
    {
        await SeedAsync(stored);

        await using var context = _database.CreateContext();
        var player = await new PlayerRepository(context)
            .GetByUsernameAsync(lookup, CancellationToken.None);

        Assert.NotNull(player);
        Assert.Equal(stored, player!.Username);
    }

    [Fact]
    public async Task GetByUsernameAsync_PreservesTheCasingUsedAtRegistration()
    {
        await SeedAsync("Ci_Keep_B");

        await using var context = _database.CreateContext();
        var player = await new PlayerRepository(context)
            .GetByUsernameAsync("ci_keep_b", CancellationToken.None);

        Assert.NotNull(player);
        Assert.Equal("Ci_Keep_B", player!.Username);
    }

    [Fact]
    public async Task GetByUsernameAsync_DifferentName_ReturnsNull()
    {
        await SeedAsync("ci_other_c");

        await using var context = _database.CreateContext();
        var player = await new PlayerRepository(context)
            .GetByUsernameAsync("ci_other_cc", CancellationToken.None);

        Assert.Null(player);
    }

    [Fact]
    public async Task SaveChangesAsync_UsernameDiffersOnlyByCase_ThrowsDuplicateUsernameException()
    {
        await SeedAsync("ci_dup_d");

        await using var context = _database.CreateContext();
        context.Players.Add(NewPlayer("CI_DUP_D"));

        await Assert.ThrowsAsync<DuplicateUsernameException>(
            () => new UnitOfWork(context).SaveChangesAsync(CancellationToken.None));
    }
}