using AutoMapper;
using GameHub.Core.DTOs;
using GameHub.Core.Entities;
using GameHub.Service.Mapping;

namespace GameHub.Tests;

public class GameStateMappingProfileTests
{
    private static MapperConfiguration Configuration() =>
        new(cfg => cfg.AddProfile<GameStateMappingProfile>());

    private static IMapper Mapper() => Configuration().CreateMapper();

    private static GameItem FullLootItem() => new()
    {
        Id = 41,
        Points = 120,
        X = 12.5,
        Y = 80.0,
        AppearAtMs = 2000,
        DisappearAtMs = 7000,
        CorrectAnswer = "should-never-leak"
    };

    [Fact]
    public void Configuration_IsValid()
    {
        Configuration().AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_LootItem_CopiesAllFields()
    {
        var box = Mapper().Map<LootBoxResponse>(FullLootItem());

        Assert.Equal(41, box.Id);
        Assert.Equal(120, box.Points);
        Assert.Equal(12.5, box.X);
        Assert.Equal(80.0, box.Y);
        Assert.Equal(2000, box.AppearAtMs);
        Assert.Equal(7000, box.DisappearAtMs);
    }

    [Theory]
    [InlineData("X")]
    [InlineData("Y")]
    [InlineData("AppearAtMs")]
    [InlineData("DisappearAtMs")]
    public void Map_LootItemWithMissingField_Throws(string missing)
    {
        var item = FullLootItem();

        switch (missing)
        {
            case "X": item.X = null; break;
            case "Y": item.Y = null; break;
            case "AppearAtMs": item.AppearAtMs = null; break;
            case "DisappearAtMs": item.DisappearAtMs = null; break;
        }

        var exception = Assert.Throws<AutoMapperMappingException>(
            () => Mapper().Map<LootBoxResponse>(item));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Contains(missing, exception.InnerException!.Message);
    }

    [Fact]
    public void Map_Round_ParsesOptionsInTheirOriginalOrder()
    {
        var item = new GameItem { Id = 5, DisplayText = "לחצו על 7", Options = "3,7,1,9,5" };

        var round = Mapper().Map<RoundResponse>(item);

        Assert.Equal(5, round.Id);
        Assert.Equal("לחצו על 7", round.DisplayText);
        Assert.Equal(new[] { 3, 7, 1, 9, 5 }, round.Options);
    }

    [Fact]
    public void Map_Round_TrimsSpacesAroundOptions()
    {
        var item = new GameItem { Options = " 3 , 7 ,1" };

        var round = Mapper().Map<RoundResponse>(item);

        Assert.Equal(new[] { 3, 7, 1 }, round.Options);
    }

    [Fact]
    public void Map_Round_WithoutOptions_ReturnsEmptyList()
    {
        var item = new GameItem { DisplayText = "7 × 8", Options = null };

        var round = Mapper().Map<RoundResponse>(item);

        Assert.Empty(round.Options);
    }

    [Fact]
    public void Map_Round_WithBlankOptions_ReturnsEmptyList()
    {
        var item = new GameItem { Options = "   " };

        var round = Mapper().Map<RoundResponse>(item);

        Assert.Empty(round.Options);
    }

    [Fact]
    public void Map_QuizItem_KeepsTheExerciseText()
    {
        var item = new GameItem { Id = 9, DisplayText = "7 × 8", CorrectAnswer = "56" };

        var round = Mapper().Map<RoundResponse>(item);

        Assert.Equal("7 × 8", round.DisplayText);
        Assert.Empty(round.Options);
    }
}