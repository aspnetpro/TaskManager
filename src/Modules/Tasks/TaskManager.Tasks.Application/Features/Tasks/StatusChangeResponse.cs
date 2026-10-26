namespace TaskManager.Tasks.Application.Features.Tasks;

public sealed record StatusChangeResponse(
    TaskItemStatus From,
    TaskItemStatus To,
    Guid ChangedBy,
    DateTimeOffset ChangedAt);
