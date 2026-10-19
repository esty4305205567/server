using GameHub.Core.Results;

namespace GameHub.Tests;

public class ResultUnauthorizedTests
{
    [Fact]
    public void Unauthorized_CarriesStatusAndMessageAndIsNotSuccess()
    {
        var result = Result<string>.Unauthorized("פרטי ההתחברות שגויים");

        Assert.Equal(ResultStatus.Unauthorized, result.Status);
        Assert.Equal("פרטי ההתחברות שגויים", result.Message);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
    }
}