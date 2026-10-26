namespace TaskManager.Tasks.Application.Abstractions;

/// <summary>
/// Todas as consultas já vêm filtradas pelo workspace do usuário atual
/// (query filter no DbContext). Nenhum handler precisa lembrar disso.
/// </summary>
public interface ITaskRepository
{
    void Add(TaskItem task);

    /// <summary>Carrega com tracking, para commands. Ignora tarefas excluídas.</summary>
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Carrega sem tracking, com histórico de status, para queries.</summary>
    Task<TaskItem?> GetForReadAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Carrega uma tarefa excluída (soft delete), para restauração.</summary>
    Task<TaskItem?> GetDeletedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> ListAsync(
        TaskListFilter filter,
        CancellationToken cancellationToken = default);
}
