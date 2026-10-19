using GameHub.Service;

namespace GameHub.Tests;

public class GameCodeGeneratorTests
{
    private const int CodeLength = 5;
    private const string ConfusingCharacters = "IO01";

    [Fact]
    public void Generate_ReturnsCodeOfFiveCharacters()
    {
        var code = new GameCodeGenerator(new Random(1)).Generate();

        Assert.Equal(CodeLength, code.Length);
    }

    [Fact]
    public void Generate_UsesOnlyUppercaseLettersAndDigitsWithoutConfusingOnes()
    {
        var generator = new GameCodeGenerator(new Random(1));

        for (var i = 0; i < 500; i++)
        {
            var code = generator.Generate();

            Assert.Equal(CodeLength, code.Length);
            Assert.All(code, c =>
            {
                Assert.True(
                    char.IsAsciiLetterUpper(c) || char.IsAsciiDigit(c),
                    $"Unexpected character '{c}' in code {code}");
                Assert.DoesNotContain(c.ToString(), ConfusingCharacters);
            });
        }
    }

    [Fact]
    public void Generate_SameSeed_ProducesSameSequence()
    {
        var first = new GameCodeGenerator(new Random(5));
        var second = new GameCodeGenerator(new Random(5));

        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(first.Generate(), second.Generate());
        }
    }

    [Fact]
    public void Generate_ProducesVariedCodes()
    {
        var generator = new GameCodeGenerator(new Random(3));

        var codes = Enumerable.Range(0, 200).Select(_ => generator.Generate()).ToHashSet();

        Assert.True(codes.Count > 150, $"Only {codes.Count} distinct codes out of 200");
    }
}