using GameHub.Core.Exceptions;
using GameHub.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GameHub.Data;

public class UnitOfWork : IUnitOfWork
{
    private const string CodeIndexName = "IX_GameSessions_Code";
    private const string UsernameIndexName = "IX_Players_Username";

    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, CodeIndexName))
        {
            throw new DuplicateCodeException(ex);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, UsernameIndexName))
        {
            throw new DuplicateUsernameException(ex);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex, string constraintName)
    {
        return ex.InnerException is PostgresException pg
            && pg.SqlState == PostgresErrorCodes.UniqueViolation
            && pg.ConstraintName == constraintName;
    }
}