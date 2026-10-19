using System.ComponentModel.DataAnnotations;
using GameHub.Core.DTOs;

namespace GameHub.Tests;

public class CaptureRequestValidationTests
{
    private static List<ValidationResult> Validate(CaptureRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void ValidItemWithoutAnswer_HasNoErrors()
    {
        Assert.Empty(Validate(new CaptureRequest { ItemId = 1 }));
    }

    [Fact]
    public void ValidItemWithAnswer_HasNoErrors()
    {
        Assert.Empty(Validate(new CaptureRequest { ItemId = 40, Answer = "56" }));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void NonPositiveItemId_ReportsHebrewItemMessage(int itemId)
    {
        var errors = Validate(new CaptureRequest { ItemId = itemId });

        var error = Assert.Single(errors);
        Assert.Equal(CaptureRequest.InvalidItemMessage, error.ErrorMessage);
        Assert.Contains(nameof(CaptureRequest.ItemId), error.MemberNames);
    }

    [Fact]
    public void MissingItemId_DefaultsToZeroAndIsRejected()
    {
        var errors = Validate(new CaptureRequest());

        Assert.Contains(errors, e => e.ErrorMessage == CaptureRequest.InvalidItemMessage);
    }

    [Fact]
    public void AnswerOfExactlyMaximumLength_IsValid()
    {
        var request = new CaptureRequest
        {
            ItemId = 1,
            Answer = new string('9', CaptureRequest.MaxAnswerLength)
        };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void AnswerLongerThanMaximum_ReportsHebrewMessage()
    {
        var request = new CaptureRequest
        {
            ItemId = 1,
            Answer = new string('9', CaptureRequest.MaxAnswerLength + 1)
        };

        var error = Assert.Single(Validate(request));

        Assert.Equal(CaptureRequest.AnswerTooLongMessage, error.ErrorMessage);
        Assert.Contains(nameof(CaptureRequest.Answer), error.MemberNames);
    }
}