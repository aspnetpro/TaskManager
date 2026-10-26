using TaskManager.Tasks.Application.Abstractions;
using TaskManager.Tasks.Domain.Enums;

namespace TaskManager.Api.Requests;

public sealed record TransitionTaskRequest(TaskItemStatus Status);
