namespace GameHub.Core.Results;

public class Result<T>
{
    public ResultStatus Status { get; }
    public T? Value { get; }
    public string? Message { get; }

    public bool IsSuccess => Status == ResultStatus.Ok;

    private Result(ResultStatus status, T? value, string? message)
    {
        Status = status;
        Value = value;
        Message = message;
    }

    public static Result<T> Ok(T value) => new(ResultStatus.Ok, value, null);
    public static Result<T> BadRequest(string message) => new(ResultStatus.BadRequest, default, message);
    public static Result<T> Unauthorized(string message) => new(ResultStatus.Unauthorized, default, message);
    public static Result<T> Forbidden(string message) => new(ResultStatus.Forbidden, default, message);
    public static Result<T> NotFound(string message) => new(ResultStatus.NotFound, default, message);
    public static Result<T> Conflict(string message) => new(ResultStatus.Conflict, default, message);
}