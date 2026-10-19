using GameHub.Core.Entities;
using GameHub.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Data;

public class AppDbContext : DbContext
{
    public const string CaseInsensitiveCollation = "case_insensitive";

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<SessionPlayer> SessionPlayers => Set<SessionPlayer>();
    public DbSet<GameItem> GameItems => Set<GameItem>();
    public DbSet<GameSetting> GameSettings => Set<GameSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasCollation(
            CaseInsensitiveCollation,
            locale: "und-u-ks-level2",
            provider: "icu",
            deterministic: false);

        modelBuilder.Entity<Player>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Username)
                .IsRequired()
                .HasMaxLength(50)
                .UseCollation(CaseInsensitiveCollation);
            e.HasIndex(p => p.Username).IsUnique();
            e.Property(p => p.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<GameSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Code).IsRequired().HasMaxLength(8);
            e.HasIndex(s => s.Code).IsUnique();

            e.Property(s => s.Version).IsConcurrencyToken();

            e.HasOne(s => s.Host)
                .WithMany()
                .HasForeignKey(s => s.HostId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(s => s.Guest)
                .WithMany()
                .HasForeignKey(s => s.GuestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SessionPlayer>(e =>
        {
            e.HasKey(sp => new { sp.SessionId, sp.PlayerId });

            e.HasOne(sp => sp.Session)
                .WithMany(s => s.Players)
                .HasForeignKey(sp => sp.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(sp => sp.Player)
                .WithMany(p => p.Sessions)
                .HasForeignKey(sp => sp.PlayerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GameItem>(e =>
        {
            e.HasKey(i => i.Id);

            e.Property(i => i.Version).IsConcurrencyToken();

            e.HasOne(i => i.Session)
                .WithMany(s => s.Items)
                .HasForeignKey(i => i.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(i => new { i.SessionId, i.Order }).IsUnique();
        });

        modelBuilder.Entity<GameSetting>(e =>
        {
            e.HasKey(g => g.GameType);
            e.Property(g => g.GameType).ValueGeneratedNever();
        });
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IVersioned>())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                entry.Entity.Version = Guid.NewGuid();
        }

        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}