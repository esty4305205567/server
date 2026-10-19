using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        IPasswordHasher hasher,
        SeedOptions options,
        CancellationToken ct = default)
    {
        if (string.Equals(options.AdminUsername, options.DemoUsername, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Seed configuration error: AdminUsername and DemoUsername must be different.");
        await SeedSettingsAsync(db, ct);

        await SeedPlayerAsync(db, hasher,
            options.AdminUsername, options.AdminPassword, PlayerRole.Admin, ct);

        await SeedPlayerAsync(db, hasher,
            options.DemoUsername, options.DemoPassword, PlayerRole.Player, ct);

        await db.SaveChangesAsync(ct);
    }
   private static async Task SeedSettingsAsync(AppDbContext db, CancellationToken ct)
    {
        foreach (var gameType in Enum.GetValues<GameType>())
        {
            var exists = await db.GameSettings.AnyAsync(g => g.GameType == gameType, ct);
            if (exists) continue;

            db.GameSettings.Add(new GameSetting
            {
                GameType = gameType,
                DurationSeconds = 60,
                Speed = 3,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
    }

    private static async Task SeedPlayerAsync(
        AppDbContext db,
        IPasswordHasher hasher,
        string username,
        string password,
        PlayerRole role,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException(
                $"Seed configuration is missing a username or password for role {role}.");

        var exists = await db.Players.AnyAsync(p => p.Username == username, ct);
        if (exists) return;

        db.Players.Add(new Player
        {
            Username = username,
            PasswordHash = hasher.Hash(password),
            Role = role
        });
    }
}