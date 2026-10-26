namespace TaskManager.Tasks.Application.Features.Tasks.GetById;

public sealed record GetTaskByIdQuery(Guid TaskId) : IRequest<Result<TaskDetailsResponse>>;
