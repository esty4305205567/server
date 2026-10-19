using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Exceptions;
using GameHub.Core.Results;
using Moq;

namespace GameHub.Tests;

public class GameSessionServiceJoinTests : GameSessionServiceTestBase
{
    private const int HostId = 1;
    private const int GuestId = 2;
    private const string Code = "K7M2Q";

    private GameSession CreateWaitingSession()
    {
        return new GameSession
        {
            Id = 17,
            Code = Code,
            Type = GameType.NumberRace,
            Status = SessionStatus.Waiting,
            HostId = HostId,
            Host = new Player { Id = HostId, Username = "dana_k" },
            DurationSeconds = 60,
            Speed = 3
        };
    }

    private void SetupExistingGuestAndSession(GameSession session)
    {
        Players.Setup(p => p.GetByIdAsync(GuestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Player { Id = GuestId, Username = "yael_l" });

        Sessions.Setup(s => s.GetByCodeAsync(Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        ItemGenerator.Setup(g => g.Type).Returns(GameType.NumberRace);
        ItemGenerator.Setup(g => g.Generate(session))
            .Returns(new List<GameItem>
            {
                new() { Order = 1, DisplayText = "לחצו על 1", CorrectAnswer = "1", Points = 3 },
                new() { Order = 2, DisplayText = "לחצו על 2", CorrectAnswer = "2", Points = 3 }
            });
    }

    [Fact]
    public async Task JoinAsync_ValidJoin_StartsGameAndCreatesItems()
    {
        var session = CreateWaitingSession();
        SetupExistingGuestAndSession(session);

        var service = CreateService();

        var result = await service.JoinAsync(Code, GuestId, CancellationToken.None);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.NotNull(result.Value);
        Assert.Equal(SessionStatus.InProgress, result.Value!.Status);
        Assert.Equal(Now, result.Value.StartedAtUtc);
        Assert.Equal("dana_k", result.Value.HostUsername);

        Assert.Equal(GuestId, session.GuestId);
        Assert.Equal(SessionStatus.InProgress, session.Status);
        Assert.Equal(Now, session.StartedAtUtc);
        Assert.Equal(2, session.Items.Count);
        Assert.Contains(session.Players, sp => sp.PlayerId == GuestId);

        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task JoinAsync_ConcurrencyConflictOnSave_ReturnsConflict()
    {
        var session = CreateWaitingSession();
        SetupExistingGuestAndSession(session);

        UnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConcurrencyConflictException());

        var service = CreateService();

        var result = await service.JoinAsync(Code, GuestId, CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal("היריב הקדים אותך, המשחק כבר מלא", result.Message);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task JoinAsync_SessionAlreadyFull_ReturnsConflictWithoutSaving()
    {
        var session = CreateWaitingSession();
        session.GuestId = 99;
        session.Status = SessionStatus.InProgress;
        SetupExistingGuestAndSession(session);

        var service = CreateService();

        var result = await service.JoinAsync(Code, GuestId, CancellationToken.None);

        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.Equal("המשחק כבר מלא", result.Message);

        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task JoinAsync_EmptyCode_ReturnsBadRequest(string code)
    {
        var service = CreateService();

        var result = await service.JoinAsync(code, GuestId, CancellationToken.None);

        Assert.Equal(ResultStatus.BadRequest, result.Status);
        Assert.Equal("יש להזין קוד משחק", result.Message);

        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task JoinAsync_PlayerNotFound_ReturnsNotFound()
    {
        Players.Setup(p => p.GetByIdAsync(GuestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Player?)null);

        var service = CreateService();

        var result = await service.JoinAsync(Code, GuestId, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("המשתמש לא נמצא", result.Message);

        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task JoinAsync_CodeNotFound_ReturnsNotFound()
    {
        Players.Setup(p => p.GetByIdAsync(GuestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Player { Id = GuestId, Username = "yael_l" });

        Sessions.Setup(s => s.GetByCodeAsync(Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GameSession?)null);

        var service = CreateService();

        var result = await service.JoinAsync(Code, GuestId, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.Equal("קוד המשחק לא נמצא", result.Message);

        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task JoinAsync_HostJoinsOwnSession_ReturnsBadRequest()
    {
        var session = CreateWaitingSession();

        Players.Setup(p => p.GetByIdAsync(HostId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Player { Id = HostId, Username = "dana_k" });

        Sessions.Setup(s => s.GetByCodeAsync(Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var service = CreateService();

        var result = await service.JoinAsync(Code, HostId, CancellationToken.None);

        Assert.Equal(ResultStatus.BadRequest, result.Status);
        Assert.Equal("אי אפשר להצטרף למשחק שפתחת בעצמך", result.Message);

        UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}