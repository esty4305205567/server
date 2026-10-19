using GameHub.Core.DTOs;
using GameHub.Core.Enums;
using Moq;

namespace GameHub.Tests;

public class GameFinisherTests : GameFinisherTestBase
{
    [Fact]
    public async Task FinishAsync_HigherScoreWins()
    {
        var session = Session();
        SetupPoints(new() { [HostId] = 9, [GuestId] = 3 });

        await CreateFinisher().FinishAsync(session, null, CancellationToken.None);

        Assert.Equal(SessionStatus.Finished, session.Status);
        Assert.Equal(Now, session.FinishedAtUtc);
        Assert.Equal(9, Player(session, HostId).Score);
        Assert.Equal(3, Player(session, GuestId).Score);
        Assert.True(Player(session, HostId).IsWinner);
        Assert.False(Player(session, GuestId).IsWinner);
    }

    [Fact]
    public async Task FinishAsync_EqualScores_NoWinner()
    {
        var session = Session();
        SetupPoints(new() { [HostId] = 6, [GuestId] = 6 });

        await CreateFinisher().FinishAsync(session, null, CancellationToken.None);

        Assert.False(Player(session, HostId).IsWinner);
        Assert.False(Player(session, GuestId).IsWinner);
    }

    [Fact]
    public async Task FinishAsync_NothingCaptured_ScoresZeroAndNoWinner()
    {
        var session = Session();
        SetupPoints(new());

        await CreateFinisher().FinishAsync(session, null, CancellationToken.None);

        Assert.Equal(0, Player(session, HostId).Score);
        Assert.Equal(0, Player(session, GuestId).Score);
        Assert.False(Player(session, HostId).IsWinner);
        Assert.False(Player(session, GuestId).IsWinner);
    }

    [Fact]
    public async Task FinishAsync_PendingCapture_IsAddedToScore()
    {
        var session = Session();
        SetupPoints(new() { [HostId] = 6, [GuestId] = 6 });

        await CreateFinisher().FinishAsync(
            session, new PendingCapture(GuestId, 3), CancellationToken.None);

        Assert.Equal(9, Player(session, GuestId).Score);
        Assert.True(Player(session, GuestId).IsWinner);
        Assert.False(Player(session, HostId).IsWinner);
    }

    [Fact]
    public async Task FinishAsync_PendingCapture_ForPlayerWithNoPoints()
    {
        var session = Session();
        SetupPoints(new());

        await CreateFinisher().FinishAsync(
            session, new PendingCapture(HostId, 3), CancellationToken.None);

        Assert.Equal(3, Player(session, HostId).Score);
        Assert.True(Player(session, HostId).IsWinner);
    }

    [Fact]
    public async Task FinishAsync_AlreadyFinished_DoesNothing()
    {
        var session = Session();
        session.Status = SessionStatus.Finished;
        Player(session, HostId).Score = 5;

        await CreateFinisher().FinishAsync(session, null, CancellationToken.None);

        Assert.Equal(5, Player(session, HostId).Score);
        Assert.Null(session.FinishedAtUtc);
        Items.Verify(i => i.GetPointsByPlayerAsync(
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}