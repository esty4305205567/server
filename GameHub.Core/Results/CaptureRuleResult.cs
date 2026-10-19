namespace GameHub.Core.Results;

public class CaptureRuleResult
{
    public ResultStatus Status { get; }
    public string? Message { get; }

    public bool IsAllowed => Status == ResultStatus.Ok;

    private CaptureRuleResult(ResultStatus status, string? message)
    {
        Status = status;
        Message = message;
    }

    public static CaptureRuleResult Allowed() => new(ResultStatus.Ok, null);
    public static CaptureRuleResult BadRequest(string message) => new(ResultStatus.BadRequest, message);
    public static CaptureRuleResult Conflict(string message) => new(ResultStatus.Conflict, message);
}