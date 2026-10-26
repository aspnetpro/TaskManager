namespace TaskManager.Shared.Domain.Results;

/// <summary>
/// Encapsula sucesso (com valor) ou falha sem exceções. Retorno de todos os handlers.
/// </summary>
public sealed class Result<T> : IResultFactory<Result<T>>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T value)
    {
        _value = value;
        _error = null;
    }

    private Result(Error error)
    {
        _value = default;
        _error = error;
    }

    public bool IsSuccess => _error is null;
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    public Error Error => _error
        ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(error);
    }

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error);
}
