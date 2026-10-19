using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Data.Repositories;

namespace GameHub.Tests;

public class GameSettingRepositoryTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;

    public GameSettingRepositoryTests(PostgresTestDatabase database)
    {
        _database = database;
    }

    private async Task SeedAsync(GameType type, int duration, int speed)
    {
        await using var context = _database.CreateContext();

        var existing = await context.GameSettings.FindAsync(type);
        if (existing is null)
        {
            context.GameSettings.Add(new GameSetting
            {
                GameType = type,
                DurationSeconds = duration,
                Speed = speed,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.DurationSeconds = duration;
            existing.Speed = speed;
        }

        await context.SaveChangesAsync();
    }

    private async Task<GameSetting> ReadFreshAsync(GameType type)
    {
        await using var context = _database.CreateContext();
        var setting = await new GameSettingRepository(context).GetAsync(type, CancellationToken.None);
        return setting!;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEverySettingOrderedByGameType()
    {
        await SeedAsync(GameType.QuickQuiz, 60, 3);
        await SeedAsync(GameType.LootCollection, 60, 3);
        await SeedAsync(GameType.NumberRace, 60, 3);

        await using var context = _database.CreateContext();
        var all = await new GameSettingRepository(context).GetAllAsync(CancellationToken.None);

        Assert.Equal(
            new[] { GameType.LootCollection, GameType.NumberRace, GameType.QuickQuiz },
            all.Select(s => s.GameType));
    }

    [Fact]
    public async Task GetForUpdateAsync_ReturnsTrackedEntity_SoChangesArePersisted()
    {
        await SeedAsync(GameType.LootCollection, 60, 3);

        await using (var context = _database.CreateContext())
        {
            var setting = await new GameSettingRepository(context)
                .GetForUpdateAsync(GameType.LootCollection, CancellationToken.None);

            setting!.DurationSeconds = 120;
            await context.SaveChangesAsync();
        }

        var stored = await ReadFreshAsync(GameType.LootCollection);
        Assert.Equal(120, stored.DurationSeconds);
    }

    [Fact]
    public async Task GetForUpdateAsync_UnknownType_ReturnsNull()
    {
        await using var context = _database.CreateContext();

        var setting = await new GameSettingRepository(context)
            .GetForUpdateAsync((GameType)99, CancellationToken.None);

        Assert.Null(setting);
    }

    [Fact]
    public async Task GetAsync_ReturnsUntrackedEntity_SoChangesAreNotPersisted()
    {
        await SeedAsync(GameType.NumberRace, 60, 3);

        await using (var context = _database.CreateContext())
        {
            var setting = await new GameSettingRepository(context)
                .GetAsync(GameType.NumberRace, CancellationToken.None);

            setting!.DurationSeconds = 999;
            await context.SaveChangesAsync();
        }

        var stored = await ReadFreshAsync(GameType.NumberRace);
        Assert.Equal(60, stored.DurationSeconds);
    }
}