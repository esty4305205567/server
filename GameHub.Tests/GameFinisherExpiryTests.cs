using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameHub.Tests;

public class GameFinisherExpiryTests : GameFinisherTestBase
{
	private static readonly DateTime Start = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

	private GameSession ExpiredSession()
	{
		var session = Session();
		session.DurationSeconds = 60;
		session.StartedAtUtc = Start;
		Clock.Setup(c => c.UtcNow).Returns(Start.AddSeconds(61));
		return session;
	}

	[Fact]
	public async Task FinishIfExpiredAsync_TimeNotUp_ReturnsSameSessionWithoutSaving()
	{
		var session = Session();
		session.DurationSeconds = 60;
		session.StartedAtUtc = Start;
		Clock.Setup(c => c.UtcNow).Returns(Start.AddSeconds(59));

		var result = await CreateFinisher().FinishIfExpiredAsync(session, CancellationToken.None);

		Assert.Same(session, result);
		Assert.Equal(SessionStatus.InProgress, session.Status);
		UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
		Items.Verify(i => i.GetPointsByPlayerAsync(
			It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task FinishIfExpiredAsync_TimeUp_FinishesAndSaves()
	{
		var session = ExpiredSession();
		SetupPoints(new() { [HostId] = 9, [GuestId] = 3 });

		var result = await CreateFinisher().FinishIfExpiredAsync(session, CancellationToken.None);

		Assert.Same(session, result);
		Assert.Equal(SessionStatus.Finished, session.Status);
		Assert.True(Player(session, HostId).IsWinner);
		UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task FinishIfExpiredAsync_ConcurrencyConflict_ReloadsAndLogsWarning()
	{
		var stale = ExpiredSession();
		SetupPoints(new() { [HostId] = 9, [GuestId] = 3 });

		var fresh = Session();
		fresh.Status = SessionStatus.Finished;

		UnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
			.ThrowsAsync(new ConcurrencyConflictException());
		Sessions.Setup(s => s.GetByIdAsync(SessionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(fresh);

		var result = await CreateFinisher().FinishIfExpiredAsync(stale, CancellationToken.None);

		Assert.Same(fresh, result);
		Assert.Equal(SessionStatus.Finished, result.Status);
		Sessions.Verify(s => s.Detach(stale), Times.Once);
		Assert.Contains(Logger.Entries, e => e.Level == LogLevel.Warning);
		Assert.DoesNotContain(Logger.Entries, e => e.Level == LogLevel.Error);
	}

	[Fact]
	public async Task FinishIfExpiredAsync_SessionStillWaiting_IsNotTouched()
	{
		var session = Session();
		session.Status = SessionStatus.Waiting;
		Clock.Setup(c => c.UtcNow).Returns(Start.AddDays(1));

		var result = await CreateFinisher().FinishIfExpiredAsync(session, CancellationToken.None);

		Assert.Same(session, result);
		Assert.Equal(SessionStatus.Waiting, session.Status);
		UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task FinishIfExpiredAsync_AlreadyFinished_IsNotTouched()
	{
		var session = ExpiredSession();
		session.Status = SessionStatus.Finished;

		var result = await CreateFinisher().FinishIfExpiredAsync(session, CancellationToken.None);

		Assert.Same(session, result);
		UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task FinishIfExpiredAsync_ConflictAndSessionVanished_Throws()
	{
		var stale = ExpiredSession();
		SetupPoints(new());
		UnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
			.ThrowsAsync(new ConcurrencyConflictException());
		Sessions.Setup(s => s.GetByIdAsync(SessionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync((GameSession?)null);

		await Assert.ThrowsAsync<InvalidOperationException>(
			() => CreateFinisher().FinishIfExpiredAsync(stale, CancellationToken.None));
	}
}