namespace CJDSL.Domain.Shared;

/// <summary>
/// 操作结果模式
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }
    public string? ErrorCode { get; }

    protected Result(bool isSuccess, string error, string? errorCode = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string error, string? errorCode = null) => new(false, error, errorCode);

    public static Result<T> Success<T>(T value) => new(true, value, string.Empty);
    public static Result<T> Failure<T>(string error, string? errorCode = null) => new(false, default!, error, errorCode);
}

public class Result<T> : Result
{
    public T Value { get; }

    protected internal Result(bool isSuccess, T value, string error, string? errorCode = null)
        : base(isSuccess, error, errorCode)
    {
        Value = value;
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }
}
