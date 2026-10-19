using GameHub.Service;

namespace GameHub.Tests;

public class LootTimingTests
{
    private readonly LootTiming _timing = new();

    [Theory]
    [InlineData(1, 8000)]
    [InlineData(2, 6000)]
    [InlineData(3, 5000)]
    [InlineData(4, 4000)]
    [InlineData(5, 3000)]
    public void GetDisplayMs_ValidSpeed_ReturnsTableValue(int speed, int expectedMs)
    {
        Assert.Equal(expectedMs, _timing.GetDisplayMs(speed));
    }

    [Theory]
    [InlineData(0, 8000)]
    [InlineData(-3, 8000)]
    [InlineData(9, 3000)]
    public void GetDisplayMs_SpeedOutOfRange_IsClamped(int speed, int expectedMs)
    {
        Assert.Equal(expectedMs, _timing.GetDisplayMs(speed));
    }
}