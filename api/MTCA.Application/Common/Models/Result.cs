namespace MTCA.Application.Common.Models;

public class Result : IResultResponse
{
    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Error> Errors { get; }
    public Error Error => Errors.Count > 0 ? Errors[0] : Error.None;

    public static Result Success() => new(true, Array.Empty<Error>());
    public static Result Failure(Error error) => new(false, new[] { error });
    public static Result Failure(IEnumerable<Error> errors) => new(false, errors.ToArray());

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
    public static Result<T> Failure<T>(IEnumerable<Error> errors) => Result<T>.Failure(errors);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, IReadOnlyList<Error> errors)
        : base(isSuccess, errors)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed Result.");

    public static Result<T> Success(T value) => new(true, value, Array.Empty<Error>());
    public static new Result<T> Failure(Error error) => new(false, default, new[] { error });
    public static new Result<T> Failure(IEnumerable<Error> errors) => new(false, default, errors.ToArray());

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);

    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsSuccess ? Result<TOut>.Success(map(Value)) : Result<TOut>.Failure(Errors);

    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> bind) =>
        IsSuccess ? bind(Value) : Result<TOut>.Failure(Errors);
}
