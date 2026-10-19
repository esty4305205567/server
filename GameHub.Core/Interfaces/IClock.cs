namespace GameHub.Core.Interfaces;

public interface IClock
{
    DateTime UtcNow { get; }
}