namespace MasterNet.Application.Core;

public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict,
    Unauthorized
}

public class Result<T>
{
    public bool IsSuccess { get; set; }
    public T? Value { get; set; }
    public string? Error { get; set; }
    public ErrorType Type { get; set; } = ErrorType.Failure;

    // Optional: detailed validation errors payload
    public IReadOnlyList<ValidationError>? ValidationErrors { get; set; }

    public static Result<T> Success(T value) => new Result<T>
    {
        IsSuccess = true,
        Value = value
    };

    public static Result<T> Failure(string error) => new Result<T>
    {
        IsSuccess = false,
        Error = error,
        Type = ErrorType.Failure
    };

    public static Result<T> NotFound(string error) => new Result<T>
    {
        IsSuccess = false,
        Error = error,
        Type = ErrorType.NotFound
    };

    public static Result<T> Conflict(string error) => new Result<T>
    {
        IsSuccess = false,
        Error = error,
        Type = ErrorType.Conflict
    };

    public static Result<T> Unauthorized(string error) => new Result<T>
    {
        IsSuccess = false,
        Error = error,
        Type = ErrorType.Unauthorized
    };

    // New: failure overload carrying a list of validation errors
    public static Result<T> Failure(IEnumerable<ValidationError> errors) => new Result<T>
    {
        IsSuccess = false,
        ValidationErrors = errors.ToList(),
        Type = ErrorType.Validation
    };
}
