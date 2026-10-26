namespace TaskManager.Tasks.Infrastructure.Persistence;

internal sealed class TaskRepository(TasksDbContext context) : ITaskRepository
{
    private static readonly string[] SoftDeleteOnly = [TaskQueryFilters.SoftDelete];

    public void Add(TaskItem task) => context.Tasks.Add(task);

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Tasks.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<TaskItem?> GetForReadAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Tasks
            .AsNoTracking()
            .Include(t => t.StatusHistory)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<TaskItem?> GetDeletedByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Tasks
            .IgnoreQueryFilters(SoftDeleteOnly) // tenant continua ativo
            .FirstOrDefaultAsync(t => t.Id == id && t.IsDeleted, cancellationToken);

    public async Task<(IReadOnlyList<TaskItem> Items, int TotalCount)> ListAsync(
        TaskListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = filter.IncludeDeleted
            ? context.Tasks.IgnoreQueryFilters(SoftDeleteOnly)
            : context.Tasks;

        query = query.AsNoTracking();

        if (filter.Status is { } status)
            query = query.Where(t => t.Status == status);

        if (filter.Priority is { } priority)
            query = query.Where(t => t.Priority == priority);

        if (filter.AssigneeId is { } assigneeId)
            query = query.Where(t => t.AssigneeId == assigneeId);

        if (filter.SearchTerm is { } term)
            query = query.Where(t =>
                t.Title.Value.Contains(term) ||
                (t.Description != null && t.Description.Value.Contains(term)));

        // US-008: vencida = due date antes de hoje e tarefa ainda aberta.
        if (filter.OverdueOn is { } today)
            query = query.Where(t =>
                t.DueDate < today &&
                t.Status != TaskItemStatus.Done &&
                t.Status != TaskItemStatus.Cancelled);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await ApplySort(query, filter)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    private static IQueryable<TaskItem> ApplySort(IQueryable<TaskItem> query, TaskListFilter filter)
    {
        var asc = filter.SortDirection == SortDirection.Asc;

        var ordered = filter.SortBy switch
        {
            TaskSortField.Priority => asc
                ? query.OrderBy(t => t.Priority)
                : query.OrderByDescending(t => t.Priority),
            TaskSortField.UpdatedAt => asc
                ? query.OrderBy(t => t.UpdatedAt ?? t.CreatedAt)
                : query.OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt),
            // Sem due date vai sempre para o fim, em qualquer direção.
            TaskSortField.DueDate => asc
                ? query.OrderBy(t => t.DueDate == null).ThenBy(t => t.DueDate)
                : query.OrderBy(t => t.DueDate == null).ThenByDescending(t => t.DueDate),
            _ => asc
                ? query.OrderBy(t => t.CreatedAt)
                : query.OrderByDescending(t => t.CreatedAt)
        };

        // Desempate estável para paginação (Guid v7 é ordenável no tempo).
        return asc ? ordered.ThenBy(t => t.Id) : ordered.ThenByDescending(t => t.Id);
    }
}
