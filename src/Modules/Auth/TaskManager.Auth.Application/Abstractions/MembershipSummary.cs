namespace TaskManager.Auth.Application.Abstractions;

public sealed record MembershipSummary(Guid WorkspaceId, string WorkspaceName, WorkspaceRole Role);
