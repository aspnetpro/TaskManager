namespace TaskManager.Tasks.Domain.Errors;

public static class TaskErrors
{
    public static Error NotFound(Guid taskId) =>
        Error.NotFound("Task.NotFound", $"Task '{taskId}' was not found.");

    public static Error InvalidTransition(TaskItemStatus from, TaskItemStatus to) =>
        Error.BusinessRule("Task.InvalidTransition", $"Cannot move a task from {from} to {to}.");

    public static readonly Error AlreadyDeleted =
        Error.BusinessRule("Task.AlreadyDeleted", "The task is already deleted.");

    public static readonly Error NotDeleted =
        Error.BusinessRule("Task.NotDeleted", "Only deleted tasks can be restored.");

    public static readonly Error RestoreWindowExpired =
        Error.BusinessRule("Task.RestoreWindowExpired",
            $"Deleted tasks can only be restored within {TaskItem.RestoreWindowDays} days.");

    public static readonly Error ClosedTaskAssignment =
        Error.BusinessRule("Task.ClosedTaskAssignment", "Done or cancelled tasks cannot be reassigned.");

    public static readonly Error EditForbidden =
        Error.Forbidden("Task.EditForbidden", "Only the reporter or the assignee can edit this task.");

    public static readonly Error AssignForbidden =
        Error.Forbidden("Task.AssignForbidden", "Developers can only assign tasks to themselves.");

    public static readonly Error DeleteForbidden =
        Error.Forbidden("Task.DeleteForbidden", "Only workspace admins can delete or restore tasks.");

    public static readonly Error ViewDeletedForbidden =
        Error.Forbidden("Task.ViewDeletedForbidden", "Only workspace admins can list deleted tasks.");

    public static readonly Error AssigneeNotMember =
        Error.Validation("Task.AssigneeNotMember", "The assignee is not an active member of this workspace.");
}
