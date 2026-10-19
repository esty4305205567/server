using GameHub.Core.Interfaces;

namespace GameHub.Service;

public class GameCodeGenerator : IGameCodeGenerator
{
    private const int CodeLength = 5;
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly Random _random;

    public GameCodeGenerator(Random random)
    {
        _random = random;
    }

    public string Generate()
    {
        var chars = new char[CodeLength];

        for (var i = 0; i < CodeLength; i++)
        {
            chars[i] = Alphabet[_random.Next(Alphabet.Length)];
        }

        return new string(chars);
    }
}