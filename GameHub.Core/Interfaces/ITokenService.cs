using GameHub.Core.Entities;

namespace GameHub.Core.Interfaces;

public interface ITokenService
{
    string CreateToken(Player player);
}