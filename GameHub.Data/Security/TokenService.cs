using System.Globalization;
using System.Security.Claims;
using System.Text;
using GameHub.Core.Entities;
using GameHub.Core.Interfaces;
using GameHub.Core.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace GameHub.Data.Security;

public class TokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly IClock _clock;
    private readonly JsonWebTokenHandler _handler = new();

    public TokenService(IOptions<JwtOptions> options, IClock clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public string CreateToken(Player player)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var now = _clock.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier,
                    player.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim(ClaimTypes.Name, player.Username),
                new Claim(ClaimTypes.Role, player.Role.ToString())
            }),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            NotBefore = now,
            Expires = now.AddMinutes(_options.ExpiresMinutes),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return _handler.CreateToken(descriptor);
    }
}