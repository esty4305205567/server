using System.Security.Claims;
using System.Text;
using GameHub.Core.Entities;
using GameHub.Core.Enums;
using GameHub.Core.Interfaces;
using GameHub.Core.Options;
using GameHub.Data.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace GameHub.Tests;

public class TokenServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc);

    private readonly JwtOptions _options = new()
    {
        Key = new string('k', JwtOptions.MinKeyLength),
        Issuer = "GameHub",
        Audience = "GameHub.Client",
        ExpiresMinutes = 60
    };

    private readonly Mock<IClock> _clock = new();

    public TokenServiceTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
    }

    private TokenService CreateService() => new(Options.Create(_options), _clock.Object);

    private static Player Player(PlayerRole role = PlayerRole.Player) =>
        new() { Id = 7, Username = "dana_k", Role = role };

    private TokenValidationParameters ValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = _options.Issuer,
        ValidateAudience = true,
        ValidAudience = _options.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
        ValidateLifetime = false
    };

    private async Task<ClaimsIdentity> ValidateAsync(string token)
    {
        var result = await new JsonWebTokenHandler()
            .ValidateTokenAsync(token, ValidationParameters());

        Assert.True(result.IsValid, result.Exception?.Message);
        return result.ClaimsIdentity;
    }

    [Fact]
    public async Task CreateToken_ContainsPlayerIdUsernameAndRoleClaims()
    {
        var token = CreateService().CreateToken(Player());

        var identity = await ValidateAsync(token);

        Assert.Equal("7", identity.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("dana_k", identity.FindFirst(ClaimTypes.Name)?.Value);
        Assert.Equal("Player", identity.FindFirst(ClaimTypes.Role)?.Value);
    }

    [Fact]
    public async Task CreateToken_AdminRole_IsWrittenAsAdmin()
    {
        var token = CreateService().CreateToken(Player(PlayerRole.Admin));

        var identity = await ValidateAsync(token);

        Assert.Equal("Admin", identity.FindFirst(ClaimTypes.Role)?.Value);
        Assert.True(new ClaimsPrincipal(identity).IsInRole("Admin"));
    }

    [Fact]
    public void CreateToken_SetsIssuerAudienceAndExpiryFromOptionsAndClock()
    {
        var token = new JsonWebTokenHandler()
            .ReadJsonWebToken(CreateService().CreateToken(Player()));

        Assert.Equal("GameHub", token.Issuer);
        Assert.Contains("GameHub.Client", token.Audiences);
        Assert.Equal(Now, token.ValidFrom);
        Assert.Equal(Now.AddMinutes(60), token.ValidTo);
    }

    [Fact]
    public async Task CreateToken_ValidatedWithWrongKey_IsRejected()
    {
        var token = CreateService().CreateToken(Player());
        var parameters = ValidationParameters();
        parameters.IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(new string('x', JwtOptions.MinKeyLength)));

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token, parameters);

        Assert.False(result.IsValid);
    }
}