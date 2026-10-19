using GameHub.Core.Interfaces;

namespace GameHub.Service;

public class LootTiming : ILootTiming
{
    private const int MinDisplayMs = 3000;

    private static readonly int[] DisplayMsBySpeed = { 8000, 6000, 5000, 4000, 3000 };

    public int GetDisplayMs(int speed)
    {
        var clamped = Math.Clamp(speed, 1, DisplayMsBySpeed.Length);
        return Math.Max(DisplayMsBySpeed[clamped - 1], MinDisplayMs);
    }
}