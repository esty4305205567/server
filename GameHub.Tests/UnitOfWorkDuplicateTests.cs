using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Exceptions;
using GameHub.Data;

namespace GameHub.Tests;

public class UnitOfWorkDuplicateTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;

    public UnitOfWorkDuplicateTests(PostgresTestDatabase database)
    {
        _database = database;
    }

    private static Player NewPlayer(string username) =>
        new() { Username = username, PasswordHash = "x", Role = PlayerRole.Player };

    private async Task<int> SeedPlayerAsync(string username)
    {
        await using var seed = _database.CreateContext();
        var player = NewPlayer(username);
        seed.Players.Add(player);
        await seed.SaveChangesAsync();
        return player.Id;
    }

    [Fact]
    public async Task SaveChangesAsync_DuplicateUsername_ThrowsDuplicateUsernameException()
    {
        await SeedPlayerAsync("dup_user_1");

        await using var context = _database.CreateContext();
        context.Players.Add(NewPlayer("dup_user_1"));

        await Assert.ThrowsAsync<DuplicateUsernameException>(
            () => new UnitOfWork(context).SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SaveChangesAsync_DifferentUsernames_Succeeds()
    {
        await SeedPlayerAsync("dup_user_2a");

        await using var context = _database.CreateContext();
        context.Players.Add(NewPlayer("dup_user_2b"));

        await new UnitOfWork(context).SaveChangesAsync(CancellationToken.None);
    }

    [Fact]
    public async Task SaveChangesAsync_DuplicateSessionCode_StillThrowsDuplicateCodeException()
    {
        var hostId = await SeedPlayerAsync("dup_user_3");

        await using (var seed = _database.CreateContext())
        {
            seed.GameSessions.Add(NewSession("DUPC1", hostId));
            await seed.SaveChangesAsync();
        }

        await using var context = _database.CreateContext();
        context.GameSessions.Add(NewSession("DUPC1", hostId));

        await Assert.ThrowsAsync<DuplicateCodeException>(
            () => new UnitOfWork(context).SaveChangesAsync(CancellationToken.None));
    }

    private static GameSession NewSession(string code, int hostId) => new()
    {
        Code = code,
        Type = GameType.NumberRace,
        Status = SessionStatus.Waiting,
        HostId = hostId,
        DurationSeconds = 60,
        Speed = 3,
        CreatedAtUtc = DateTime.UtcNow
    };
}