namespace GameHub.Core.Interfaces;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken ct);
}