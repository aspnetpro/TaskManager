namespace TaskManager.Shared.Domain.Results;

/// <summary>
/// Permite que código genérico (ex: ValidationBehavior, TracingBehavior) crie
/// e inspecione o tipo concreto sem reflection nem cast inseguro.
/// </summary>
public interface IResultFactory<TSelf>
    where TSelf : IResultFactory<TSelf>
{
    bool IsFailure { get; }

    Error Error { get; }

    static abstract TSelf Failure(Error error);
}
