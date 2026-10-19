using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Exceptions;
using GameHub.Data;
using GameHub.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GameHub.Tests;

public class GameSessionDetachTests : IClassFixture<PostgresTestDatabase>
{
    private readonly PostgresTestDatabase _database;

    public GameSessionDetachTests(PostgresTestDatabase database)
    {
        _database = database;
    }

    private static GameSession NewSession(string code, int hostId)
    {
        var session = new GameSession
        {
            Code = code,
            Type = GameType.QuickQuiz,
            Status = SessionStatus.Waiting,
            HostId = hostId,
            DurationSeconds = 60,
            Speed = 3,
            CreatedAtUtc = DateTime.UtcNow
        };

        session.Players.Add(new SessionPlayer { PlayerId = hostId });
        return session;
    }

    [Fact]
    public async Task Detach_AfterDuplicateCode_AllowsSavingAnotherSession()
    {
        int hostId;

        await using (var seed = _database.CreateContext())
        {
            var host = new Player { Username = "detach_host", PasswordHash = "x", Role = PlayerRole.Player };
            seed.Players.Add(host);
            await seed.SaveChangesAsync();
            hostId = host.Id;
        }

        await using var context = _database.CreateContext();
        var repository = new GameSessionRepository(context);
        var unitOfWork = new UnitOfWork(context);

        await repository.AddAsync(NewSession("AAAAA", hostId), CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        var failing = NewSession("AAAAA", hostId);
        await repository.AddAsync(failing, CancellationToken.None);

        await Assert.ThrowsAsync<DuplicateCodeException>(
            () => unitOfWork.SaveChangesAsync(CancellationToken.None));

        repository.Detach(failing);

        await repository.AddAsync(NewSession("BBBBB", hostId), CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        await using var verify = _database.CreateContext();
        var codes = await verify.GameSessions
            .AsNoTracking()
            .Where(s => s.HostId == hostId)
            .Select(s => s.Code)
            .OrderBy(c => c)
            .ToListAsync();

        Assert.Equal(new[] { "AAAAA", "BBBBB" }, codes);

        var playerRows = await verify.SessionPlayers
            .AsNoTracking()
            .CountAsync(sp => sp.PlayerId == hostId);

        Assert.Equal(2, playerRows);
    }
}