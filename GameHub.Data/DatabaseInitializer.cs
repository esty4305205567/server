using GameHub.Core.Interfaces;
using GameHub.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GameHub.Data;

public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly SeedOptions _options;

    public DatabaseInitializer(
        AppDbContext db,
        IPasswordHasher hasher,
        IOptions<SeedOptions> options)
    {
        _db = db;
        _hasher = hasher;
        _options = options.Value;
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        await _db.Database.MigrateAsync(ct);
        await DbSeeder.SeedAsync(_db, _hasher, _options, ct);
    }
}