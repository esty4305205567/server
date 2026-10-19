using GameHub.Core.Interfaces;

namespace GameHub.Service;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}