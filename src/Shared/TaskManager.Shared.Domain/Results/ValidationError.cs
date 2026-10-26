namespace TaskManager.Shared.Domain.Results;

/// <summary>
/// Erro de validação com detalhes por campo — vira ValidationProblem (400) na Api.
/// </summary>
public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Errors)
    : Error("Validation.Failed", "One or more validation errors occurred.", ErrorType.Validation);
