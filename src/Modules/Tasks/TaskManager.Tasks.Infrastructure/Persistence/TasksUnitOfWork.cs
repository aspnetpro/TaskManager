using TaskManager.Shared.Infrastructure.Persistence;

namespace TaskManager.Tasks.Infrastructure.Persistence;

internal sealed class TasksUnitOfWork(TasksDbContext context)
    : EfUnitOfWork<TasksDbContext>(context), ITasksUnitOfWork;
