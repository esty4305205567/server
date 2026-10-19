using GameHub.Core.Options;
using GameHub.Data.Security;

namespace GameHub.Tests;

public class JwtOptionsValidatorTests
{
    private readonly JwtOptionsValidator _validator = new();

    private static JwtOptions Valid() => new()
    {
        Key = new string('k', JwtOptions.MinKeyLength),
        Issuer = "GameHub",
        Audience = "GameHub.Client",
        ExpiresMinutes = 60
    };

    [Fact]
    public void Validate_ValidOptions_Succeeds()
    {
        var result = _validator.Validate(null, Valid());

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_KeyShorterThanMinimum_Fails()
    {
        var options = Valid();
        options.Key = new string('k', JwtOptions.MinKeyLength - 1);

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("Jwt:Key"));
    }

    [Fact]
    public void Validate_EmptyKey_Fails()
    {
        var options = Valid();
        options.Key = string.Empty;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("Jwt:Key"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_BlankIssuer_Fails(string issuer)
    {
        var options = Valid();
        options.Issuer = issuer;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("Jwt:Issuer"));
    }

    [Fact]
    public void Validate_EmptyAudience_Fails()
    {
        var options = Valid();
        options.Audience = string.Empty;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("Jwt:Audience"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_NonPositiveExpiry_Fails(int minutes)
    {
        var options = Valid();
        options.ExpiresMinutes = minutes;

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, f => f.Contains("Jwt:ExpiresMinutes"));
    }

    [Fact]
    public void Validate_SeveralProblems_ReportsAllOfThem()
    {
        var options = new JwtOptions { Key = "short", Issuer = "", Audience = "", ExpiresMinutes = 0 };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Equal(4, result.Failures!.Count());
    }
}