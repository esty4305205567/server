using System.Text.RegularExpressions;
using GameHub.Core.Validation;

namespace GameHub.Tests;

public class UsernameRulesTests
{
    private static bool IsValid(string value) => Regex.IsMatch(value, UsernameRules.Pattern);

    [Theory]
    [InlineData("dana")]
    [InlineData("Dana123")]
    [InlineData("A1b2C3")]
    public void Pattern_AcceptsEnglishLettersAndDigits(string username)
    {
        Assert.True(IsValid(username));
    }

    [Fact]
    public void Pattern_AcceptsExactlyMinimumLength()
    {
        Assert.True(IsValid(new string('a', UsernameRules.MinLength)));
    }

    [Fact]
    public void Pattern_AcceptsExactlyMaximumLength()
    {
        Assert.True(IsValid(new string('a', UsernameRules.MaxLength)));
    }

    [Fact]
    public void Pattern_RejectsShorterThanMinimum()
    {
        Assert.False(IsValid(new string('a', UsernameRules.MinLength - 1)));
    }

    [Fact]
    public void Pattern_RejectsLongerThanMaximum()
    {
        Assert.False(IsValid(new string('a', UsernameRules.MaxLength + 1)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("dana k")]
    [InlineData("dana_k")]
    [InlineData("dana-k")]
    [InlineData("דנה")]
    [InlineData("dana\n")]
    [InlineData(" dana")]
    [InlineData("dana ")]
    [InlineData("dana!")]
    public void Pattern_RejectsEverythingElse(string username)
    {
        Assert.False(IsValid(username));
    }
}