using GameHub.Core.DTOs;

namespace GameHub.Tests;

public class GameStateResponseContractTests
{
    [Theory]
    [InlineData(typeof(GameStateResponse))]
    [InlineData(typeof(LootBoxResponse))]
    [InlineData(typeof(RoundResponse))]
    [InlineData(typeof(PlayerStateResponse))]
    public void StateDtos_NeverExposeTheCorrectAnswer(Type dto)
    {
        var names = dto.GetProperties().Select(p => p.Name);

        Assert.DoesNotContain(names, n => n.Contains("Answer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GameStateResponse_Defaults_AreEmptyAndNotADraw()
    {
        var state = new GameStateResponse();

        Assert.Empty(state.LootBoxes);
        Assert.Null(state.CurrentRound);
        Assert.Null(state.Opponent);
        Assert.Null(state.RemainingSeconds);
        Assert.Null(state.Winner);
        Assert.False(state.IsDraw);
    }

    [Fact]
    public void RoundResponse_Defaults_HaveNoOptions()
    {
        Assert.Empty(new RoundResponse().Options);
    }
}