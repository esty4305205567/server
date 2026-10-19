using System.ComponentModel.DataAnnotations;
using GameHub.Core.DTOs;
using GameHub.Core.Validation;

namespace GameHub.Tests;

public class AuthRequestValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            model, new ValidationContext(model), results, validateAllProperties: true);
        return results;
    }

    private static RegisterRequest ValidRegister() =>
        new() { Username = "dana1", Password = "password1" };

    [Fact]
    public void Register_ValidRequest_HasNoErrors()
    {
        Assert.Empty(Validate(ValidRegister()));
    }

    [Fact]
    public void Register_InvalidUsername_ReportsHebrewUsernameMessage()
    {
        var request = ValidRegister();
        request.Username = "dana k";

        var errors = Validate(request);

        var error = Assert.Single(errors);
        Assert.Equal(UsernameRules.InvalidMessage, error.ErrorMessage);
        Assert.Contains(nameof(RegisterRequest.Username), error.MemberNames);
    }

    [Fact]
    public void Register_EmptyUsername_ReportsError()
    {
        var request = ValidRegister();
        request.Username = string.Empty;

        var errors = Validate(request);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(RegisterRequest.Username)));
    }

    [Fact]
    public void Register_PasswordShorterThanMinimum_ReportsTooShort()
    {
        var request = ValidRegister();
        request.Password = new string('p', PasswordRules.MinLength - 1);

        var errors = Validate(request);

        Assert.Contains(errors, e => e.ErrorMessage == PasswordRules.TooShortMessage);
    }

    [Fact]
    public void Register_PasswordExactlyMinimum_IsValid()
    {
        var request = ValidRegister();
        request.Password = new string('p', PasswordRules.MinLength);

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void Register_PasswordLongerThanMaximum_ReportsTooLong()
    {
        var request = ValidRegister();
        request.Password = new string('p', PasswordRules.MaxLength + 1);

        var errors = Validate(request);

        Assert.Contains(errors, e => e.ErrorMessage == PasswordRules.TooLongMessage);
    }

    [Fact]
    public void Register_PasswordExactlyMaximum_IsValid()
    {
        var request = ValidRegister();
        request.Password = new string('p', PasswordRules.MaxLength);

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void Login_ValidRequest_HasNoErrors()
    {
        var request = new LoginRequest { Username = "dana1", Password = "x" };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void Login_EmptyUsername_ReportsError()
    {
        var request = new LoginRequest { Username = string.Empty, Password = "x" };

        var errors = Validate(request);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(LoginRequest.Username)));
    }

    [Fact]
    public void Login_EmptyPassword_ReportsError()
    {
        var request = new LoginRequest { Username = "dana1", Password = string.Empty };

        var errors = Validate(request);

        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(LoginRequest.Password)));
    }

    [Fact]
    public void Login_UsernameNotMatchingRegistrationRules_IsStillValid()
    {
        var request = new LoginRequest { Username = "not valid!", Password = "x" };

        Assert.Empty(Validate(request));
    }
}