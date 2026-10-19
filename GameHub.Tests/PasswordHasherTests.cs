using GameHub.Data.Security;

namespace GameHub.Tests;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Hash_DoesNotReturnThePlainPassword()
    {
        var hash = _hasher.Hash("Secret123");

        Assert.NotEqual("Secret123", hash);
        Assert.False(string.IsNullOrWhiteSpace(hash));
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _hasher.Hash("Secret123");

        Assert.True(_hasher.Verify("Secret123", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _hasher.Hash("Secret123");

        Assert.False(_hasher.Verify("Secret124", hash));
    }

    [Fact]
    public void Verify_IsCaseSensitive()
    {
        var hash = _hasher.Hash("Secret123");

        Assert.False(_hasher.Verify("secret123", hash));
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        Assert.NotEqual(_hasher.Hash("Secret123"), _hasher.Hash("Secret123"));
    }

    [Fact]
    public void DummyHash_IsAValidHashThatMatchesNoOrdinaryPassword()
    {
        Assert.False(_hasher.Verify("password", _hasher.DummyHash));
        Assert.False(_hasher.Verify("Secret123", _hasher.DummyHash));
    }

    [Fact]
    public void Verify_PasswordsDifferingOnlyAfterTheFirst72Characters_AreTreatedAsEqual()
    {
        var prefix = new string('a', 72);
        var hash = _hasher.Hash(prefix + "X");

        Assert.True(_hasher.Verify(prefix + "Y", hash));
    }
}