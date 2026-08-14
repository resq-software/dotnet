namespace ResQ.BuildingBlocks.Domain;

/// <summary>Classifies an <see cref="Error"/> so adapters can map it (e.g. to an HTTP status).</summary>
public enum ErrorType
{
    /// <summary>A generic failure.</summary>
    Failure = 0,

    /// <summary>Input failed validation.</summary>
    Validation = 1,

    /// <summary>The requested resource was not found.</summary>
    NotFound = 2,

    /// <summary>The operation conflicts with current state (e.g. a uniqueness violation).</summary>
    Conflict = 3,

    /// <summary>The caller is not permitted to perform the operation.</summary>
    Unauthorized = 4,
}

/// <summary>A domain error: a stable <paramref name="Code"/>, a human message, and a <paramref name="Type"/>.</summary>
/// <param name="Code">Stable machine-readable code (e.g. <c>"user.not_found"</c>).</param>
/// <param name="Message">Human-readable description.</param>
/// <param name="Type">The error classification.</param>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    /// <summary>The absence of an error.</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>Creates a <see cref="ErrorType.Validation"/> error.</summary>
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    /// <summary>Creates a <see cref="ErrorType.NotFound"/> error.</summary>
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    /// <summary>Creates a <see cref="ErrorType.Conflict"/> error.</summary>
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
}

/// <summary>The outcome of an operation: success, or failure carrying an <see cref="Error"/>.</summary>
public class Result
{
    /// <summary>Creates a result. Success must have <see cref="Error.None"/>; failure must not.</summary>
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new ArgumentException("A successful result cannot carry an error.", nameof(error));
        }

        if (!isSuccess && error == Error.None)
        {
            throw new ArgumentException("A failed result must carry an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>True when the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>True when the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>The error on failure, or <see cref="Error.None"/> on success.</summary>
    public Error Error { get; }

    /// <summary>A successful result.</summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>A failed result.</summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>A successful result carrying a <paramref name="value"/>.</summary>
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);

    /// <summary>A failed result of type <typeparamref name="T"/>.</summary>
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

/// <summary>A <see cref="Result"/> that carries a <typeparamref name="T"/> value on success.</summary>
/// <typeparam name="T">The value type.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, Error error) : base(isSuccess, error) => _value = value;

    /// <summary>The value on success. Throws if accessed on a failed result.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    /// <summary>Implicitly lifts a value into a successful result.</summary>
    public static implicit operator Result<T>(T value) => Success(value);
}
