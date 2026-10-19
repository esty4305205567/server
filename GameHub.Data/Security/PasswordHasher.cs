using GameHub.Core.Interfaces;

namespace GameHub.Data.Security;

public class PasswordHasher : IPasswordHasher
{
    public PasswordHasher()
    {
        DummyHash = Hash(Guid.NewGuid().ToString("N"));
    }

    public string DummyHash { get; }

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}