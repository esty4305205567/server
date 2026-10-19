using GameHub.Core.DTOs;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Results;
using Moq;

namespace GameHub.Tests;

public class GameStateServiceTests : GameStateServiceTestBase
{
    private static GameItem LootItem(int id) => new()
    {
        Id = id,
        Points = 50,
        X = 10.0,
        Y = 20.0,
        AppearAtMs = 0,
        DisappearAtMs = 5000,
        CorrectAnswer = "never-exposed"
    };

    private async Task<Result<GameStateResponse>> GetAsync(int playerId = HostId) =>
        await CreateService().GetStateAsync(SessionId, playerId, CancellationToken.None);

    [Fact]
    public async Task GetStateAsync_SessionNotFound_ReturnsNotFound()
    {
        Sessions.Setup(s => s.GetByIdAsync(SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameSession?)null);

        var result = await GetAsync();

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("המשחק לא נמצא", result.Message);
        Finisher.Verify(f => f.FinishIfExpiredAsync(
            It.IsAny<GameSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStateAsync_PlayerNotInSession_ReturnsForbiddenWithoutTouchingTheGame()
    {
        SetupSession(Session());

        var result = await GetAsync(playerId: 99);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal("אינך משתתף במשחק הזה", result.Message);
        Finisher.Verify(f => f.FinishIfExpiredAsync(
            It.IsAny<GameSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStateAsync_Waiting_ReturnsCodeAndOnlyTheHost()
    {
        SetupSession(Session(SessionStatus.Waiting, guestId: null));

        var result = await GetAsync();

        Assert.Equal(ResultStatus.Ok, result.Status);
        var state = result.Value!;
        Assert.Equal(SessionStatus.Waiting, state.Status);
        Assert.Equal("K7M2Q", state.Code);
        Assert.Equal("dana1", state.Me.Username);
        Assert.Equal(0, state.Me.Score);
        Assert.Null(state.Opponent);
        Assert.Null(state.RemainingSeconds);
        Assert.Empty(state.LootBoxes);
        Assert.Null(state.CurrentRound);
        Assert.Null(state.Winner);
        Assert.False(state.IsDraw);

        Items.Verify(i => i.GetPointsByPlayerAsync(
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Items.Verify(i => i.GetActiveLootItemsAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Items.Verify(i => i.GetCurrentRoundItemAsync(
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStateAsync_LootCollection_ReturnsActiveBoxesForTheElapsedTime()
    {
        var session = Session(type: GameType.LootCollection);
        SetupSession(session);

        IReadOnlyList<GameItem> boxes = new List<GameItem> { LootItem(41), LootItem(42) };
        Items.Setup(i => i.GetActiveLootItemsAsync(SessionId, 10000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(boxes);

        var result = await GetAsync();

        Assert.Equal(ResultStatus.Ok, result.Status);
        var state = result.Value!;
        Assert.Equal(new[] { 41, 42 }, state.LootBoxes.Select(b => b.Id));
        Assert.Null(state.CurrentRound);
        Assert.Equal(Start.AddSeconds(10), state.ServerNowUtc);
        Assert.Equal(50, state.RemainingSeconds);

        Finisher.Verify(f => f.FinishIfExpiredAsync(session, It.IsAny<CancellationToken>()), Times.Once);
        Items.Verify(i => i.GetCurrentRoundItemAsync(
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStateAsync_NumberRace_ReturnsTheCurrentRoundWithItsOptions()
    {
        SetupSession(Session());

        var round = new GameItem
        {
            Id = 5,
            DisplayText = "לחצו על 7",
            Options = "3,7,1,9,5",
            CorrectAnswer = "7"
        };
        Items.Setup(i => i.GetCurrentRoundItemAsync(SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(round);

        var result = await GetAsync();

        var state = result.Value!;
        Assert.Equal(5, state.CurrentRound!.Id);
        Assert.Equal("לחצו על 7", state.CurrentRound.DisplayText);
        Assert.Equal(new[] { 3, 7, 1, 9, 5 }, state.CurrentRound.Options);
        Assert.Empty(state.LootBoxes);

        Items.Verify(i => i.GetActiveLootItemsAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStateAsync_NumberRaceWithNoRoundLeft_ReturnsNoCurrentRound()
    {
        SetupSession(Session());

        var result = await GetAsync();

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Null(result.Value!.CurrentRound);
    }

    [Theory]
    [InlineData(HostId, "dana1", 9, "yael1", 3)]
    [InlineData(GuestId, "yael1", 3, "dana1", 9)]
    public async Task GetStateAsync_ScoresComeFromTheCapturedItemsFromEachPlayersPointOfView(
        int viewerId, string meName, int meScore, string opponentName, int opponentScore)
    {
        SetupSession(Session());
        SetupPoints(new Dictionary<int, int> { [HostId] = 9, [GuestId] = 3 });

        var result = await GetAsync(viewerId);

        var state = result.Value!;
        Assert.Equal(meName, state.Me.Username);
        Assert.Equal(meScore, state.Me.Score);
        Assert.Equal(opponentName, state.Opponent!.Username);
        Assert.Equal(opponentScore, state.Opponent.Score);
    }

    [Theory]
    [InlineData(10000, 50)]
    [InlineData(10400, 50)]
    [InlineData(59500, 1)]
    [InlineData(60000, 0)]
    [InlineData(61000, 0)]
    public async Task GetStateAsync_RemainingSecondsAreRoundedUpAndNeverNegative(
        int elapsedMs, int expectedRemaining)
    {
        SetupSession(Session());
        Clock.Setup(c => c.UtcNow).Returns(Start.AddMilliseconds(elapsedMs));

        var result = await GetAsync();

        Assert.Equal(expectedRemaining, result.Value!.RemainingSeconds);
    }

    [Fact]
    public async Task GetStateAsync_FinishedWithWinner_ReturnsTheWinnerAndNoItems()
    {
        var loaded = Session();
        SetupSession(loaded);

        var finished = Session(SessionStatus.Finished);
        finished.Players.Single(p => p.PlayerId == HostId).IsWinner = true;
        Finisher.Setup(f => f.FinishIfExpiredAsync(loaded, It.IsAny<CancellationToken>()))
            .ReturnsAsync(finished);

        SetupPoints(new Dictionary<int, int> { [HostId] = 9, [GuestId] = 3 });

        var result = await GetAsync(GuestId);

        var state = result.Value!;
        Assert.Equal(SessionStatus.Finished, state.Status);
        Assert.Equal("dana1", state.Winner);
        Assert.False(state.IsDraw);
        Assert.Equal(0, state.RemainingSeconds);
        Assert.Equal(3, state.Me.Score);
        Assert.Equal(9, state.Opponent!.Score);
        Assert.Empty(state.LootBoxes);
        Assert.Null(state.CurrentRound);

        Items.Verify(i => i.GetActiveLootItemsAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Items.Verify(i => i.GetCurrentRoundItemAsync(
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetStateAsync_FinishedWithoutWinner_IsADraw()
    {
        var loaded = Session();
        SetupSession(loaded);

        var finished = Session(SessionStatus.Finished);
        Finisher.Setup(f => f.FinishIfExpiredAsync(loaded, It.IsAny<CancellationToken>()))
            .ReturnsAsync(finished);

        var result = await GetAsync();

        var state = result.Value!;
        Assert.Equal(SessionStatus.Finished, state.Status);
        Assert.Null(state.Winner);
        Assert.True(state.IsDraw);
    }

    [Fact]
    public async Task GetStateAsync_UsernameMissing_Throws()
    {
        SetupSession(Session());
        SetupUsernames(new Dictionary<int, string> { [HostId] = "dana1" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => GetAsync());
    }
}