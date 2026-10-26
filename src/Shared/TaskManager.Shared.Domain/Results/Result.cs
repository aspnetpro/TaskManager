namespace TaskManager.Shared.Domain.Results;

/// <summary>
/// Resultado sem valor — usado por commands que não retornam dados (ex: delete).
/// </summary>
public sealed class Result : IResultFactory<Result>
{
    private static readonly Result SuccessInstance = new(null);
    private readonly Error? _error;

    private Result(Error? error) => _error = error;

    public bool IsSuccess => _error is null;
    public bool IsFailure => !IsSuccess;

    public Error Error => _error
        ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result Success() => SuccessInstance;

    public static Result Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(error);
    }

    public static implicit operator Result(Error error) => Failure(error);

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(Error);
}
