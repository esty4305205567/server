using GameHub.Core.DTOs;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Exceptions;
using GameHub.Core.Results;
using Microsoft.Extensions.Logging;
using Moq;

namespace GameHub.Tests;

public class GameCaptureServiceCaptureTests : GameCaptureServiceTestBase
{
    private static CaptureRequest Request() => new() { ItemId = ItemId, Answer = "3" };

    private void SetupAllowedRule(int currentRound = 1)
    {
        Items.Setup(i => i.GetCurrentRoundOrderAsync(SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentRound);
        Rule.Setup(r => r.Evaluate(It.IsAny<CaptureContext>()))
            .Returns(CaptureRuleResult.Allowed());
    }

    [Fact]
    public async Task CaptureAsync_ValidCapture_MarksItemAndReturnsPoints()
    {
        var item = OpenItem();
        SetupSessionAndItem(ActiveSession(), item);
        SetupAllowedRule();
        Items.Setup(i => i.AnyOtherUncapturedAsync(SessionId, ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateService().CaptureAsync(SessionId, GuestId, Request(), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(ItemId, result.Value!.ItemId);
        Assert.Equal(3, result.Value.Points);
        Assert.False(result.Value.GameFinished);

        Assert.Equal(GuestId, item.CapturedByPlayerId);
        Assert.Equal(Start.AddSeconds(10), item.CapturedAtUtc);

        Finisher.Verify(f => f.FinishAsync(
            It.IsAny<GameSession>(), It.IsAny<PendingCapture?>(), It.IsAny<CancellationToken>()), Times.Never); UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CaptureAsync_LastRound_FinishesGame()
    {
        var session = ActiveSession();
        SetupSessionAndItem(session, OpenItem());
        SetupAllowedRule();
        Items.Setup(i => i.AnyOtherUncapturedAsync(SessionId, ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateService().CaptureAsync(SessionId, GuestId, Request(), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.True(result.Value!.GameFinished);

        Finisher.Verify(f => f.FinishAsync(
            session, new PendingCapture(GuestId, 3), It.IsAny<CancellationToken>()), Times.Once);
        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CaptureAsync_LootCollection_NeverFinishesAndSkipsRoundQuery()
    {
        var session = ActiveSession(GameType.LootCollection);
        SetupSessionAndItem(session, OpenItem());
        Rule.Setup(r => r.Type).Returns(GameType.LootCollection);
        Rule.Setup(r => r.Evaluate(It.IsAny<CaptureContext>()))
            .Returns(CaptureRuleResult.Allowed());

        var result = await CreateService().CaptureAsync(SessionId, GuestId, Request(), CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.False(result.Value!.GameFinished);

        Items.Verify(i => i.GetCurrentRoundOrderAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Items.Verify(i => i.AnyOtherUncapturedAsync(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        Finisher.Verify(f => f.FinishAsync(
            It.IsAny<GameSession>(), It.IsAny<PendingCapture?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CaptureAsync_ConcurrencyConflictOnSave_ReturnsConflictAndLogsWarning()
    {
        SetupSessionAndItem(ActiveSession(), OpenItem());
        SetupAllowedRule();
        Items.Setup(i => i.AnyOtherUncapturedAsync(SessionId, ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        UnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException());

        var result = await CreateService().CaptureAsync(SessionId, GuestId, Request(), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal("היריב הקדים אותך, הפריט כבר נתפס", result.Message);
        Assert.Null(result.Value);

        Assert.Contains(Logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.DoesNotContain(Logger.Entries, e => e.Level == LogLevel.Error);
    }

    [Fact]
    public async Task CaptureAsync_NoRuleForGameType_ReturnsNotFoundAndLogsError()
    {
        SetupSessionAndItem(ActiveSession(GameType.QuickQuiz), OpenItem());

        var result = await CreateService().CaptureAsync(SessionId, GuestId, Request(), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("סוג המשחק אינו נתמך", result.Message);
        Assert.Contains(Logger.Entries, e => e.Level == LogLevel.Error);
    }
}