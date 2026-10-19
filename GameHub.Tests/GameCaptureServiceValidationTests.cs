using GameHub.Core.DTOs;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Results;
using Moq;

namespace GameHub.Tests;

public class GameCaptureServiceValidationTests : GameCaptureServiceTestBase
{
    private static CaptureRequest Request() => new() { ItemId = ItemId };

    private void SetupFinisherEndsGame(GameSession session)
    {
        var finished = ActiveSession();
        finished.Status = SessionStatus.Finished;

        Finisher.Setup(f => f.FinishIfExpiredAsync(session, It.IsAny<CancellationToken>()))
            .ReturnsAsync(finished);
    }

    [Fact]
    public async Task CaptureAsync_SessionNotFound_ReturnsNotFound()
    {
        Sessions.Setup(s => s.GetByIdAsync(SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameSession?)null);

        var result = await CreateService().CaptureAsync(SessionId, GuestId, Request(), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("המשחק לא נמצא", result.Message);
        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CaptureAsync_ItemNotFound_ReturnsNotFound()
    {
        Sessions.Setup(s => s.GetByIdAsync(SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActiveSession());
        Items.Setup(i => i.GetByIdInSessionAsync(SessionId, ItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameItem?)null);

        var result = await CreateService().CaptureAsync(SessionId, GuestId, Request(), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("הפריט לא נמצא", result.Message);
    }

    [Fact]
    public async Task CaptureAsync_PlayerNotInSession_ReturnsForbidden()
    {
        SetupSessionAndItem(ActiveSession(), OpenItem());

        var result = await CreateService().CaptureAsync(SessionId, 99, Request(), CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal("אינך משתתף במשחק הזה", result.Message);
        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(SessionStatus.Waiting)]
    [InlineData(SessionStatus.Finished)]
    public async Task CaptureAsync_SessionNotInProgress_ReturnsConflict(SessionStatus status)
    {
        var session = ActiveSession();
        session.Status = status;
        SetupSessionAndItem(session, OpenItem());

        var result = await CreateService().CaptureAsync(SessionId, GuestId, Request(), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal("המשחק אינו פעיל", result.Message);
        Finisher.Verify(f => f.FinishIfExpiredAsync(
            It.IsAny<GameSession>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CaptureAsync_SessionExpired_FinisherEndsGame_ReturnsConflictWithoutEvaluatingRule()
    {
        var session = ActiveSession();
        SetupSessionAndItem(session, OpenItem());
        SetupFinisherEndsGame(session);

        var result = await CreateService().CaptureAsync(SessionId, GuestId, Request(), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal("המשחק הסתיים", result.Message);
        Finisher.Verify(f => f.FinishIfExpiredAsync(session, It.IsAny<CancellationToken>()), Times.Once);
        Rule.Verify(r => r.Evaluate(It.IsAny<CaptureContext>()), Times.Never);
        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CaptureAsync_SessionExpiredAndItemAlreadyCaptured_ReportsGameEnded()
    {
        var item = OpenItem();
        item.CapturedByPlayerId = HostId;
        var session = ActiveSession();
        SetupSessionAndItem(session, item);
        SetupFinisherEndsGame(session);

        var result = await CreateService().CaptureAsync(SessionId, GuestId, Request(), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal("המשחק הסתיים", result.Message);
    }

    [Fact]
    public async Task CaptureAsync_ItemAlreadyCaptured_ReturnsConflict()
    {
        var item = OpenItem();
        item.CapturedByPlayerId = HostId;
        SetupSessionAndItem(ActiveSession(), item);

        var result = await CreateService().CaptureAsync(SessionId, GuestId, Request(), CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal("היריב הקדים אותך, הפריט כבר נתפס", result.Message);
        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(ResultStatus.BadRequest)]
    [InlineData(ResultStatus.Conflict)]
    public async Task CaptureAsync_RuleRejects_ReturnsRuleStatusAndMessage(ResultStatus status)
    {
        SetupSessionAndItem(ActiveSession(), OpenItem());
        Items.Setup(i => i.GetCurrentRoundOrderAsync(SessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        Rule.Setup(r => r.Evaluate(It.IsAny<CaptureContext>()))
            .Returns(status == ResultStatus.Conflict
                ? CaptureRuleResult.Conflict("נדחה")
                : CaptureRuleResult.BadRequest("נדחה"));

        var result = await CreateService().CaptureAsync(SessionId, GuestId, Request(), CancellationToken.None);

        Assert.Equal(status, result.Status);
        Assert.Equal("נדחה", result.Message);
        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}