using TaskManager.Shared.Domain;
using TaskManager.Shared.Domain.Results;

namespace TaskManager.Shared.Application;

/// <summary>
/// Unidade de trabalho. Cada módulo expõe a sua própria interface derivada
/// (ex: ITasksUnitOfWork) — uma IUnitOfWork única no DI faria o último
/// módulo registrado "ganhar" e salvar no DbContext errado.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persiste as mudanças. Conflito de concorrência vira Result com ErrorType.Conflict.
    /// </summary>
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default);
}
