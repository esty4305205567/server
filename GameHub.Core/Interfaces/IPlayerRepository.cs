using GameHub.Core.Entities;

namespace GameHub.Core.Interfaces;

public interface IPlayerRepository
{
    Task<Player?> GetByIdAsync(int id, CancellationToken ct);

    Task<Player?> GetByUsernameAsync(string username, CancellationToken ct);

    Task<IReadOnlyDictionary<int, string>> GetUsernamesAsync(
        IReadOnlyCollection<int> ids, CancellationToken ct);

    Task AddAsync(Player player, CancellationToken ct);
}