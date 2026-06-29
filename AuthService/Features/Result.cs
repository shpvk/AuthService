namespace AuthService.Features;

public sealed record Error(string Code, string Description);

public sealed record Unit
{
    public static readonly Unit Value = new();
}

public sealed class Result<T>
{
    private Result(T? value, IReadOnlyCollection<Error> errors, bool isSuccess)
    {
        Value = value;
        Errors = errors;
        IsSuccess = isSuccess;
    }

    public T? Value { get; }

    public IReadOnlyCollection<Error> Errors { get; }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public static Result<T> Success(T value) => new(value, Array.Empty<Error>(), true);

    public static Result<T> Failure(IEnumerable<Error> errors) => new(default, errors.ToArray(), false);
}
