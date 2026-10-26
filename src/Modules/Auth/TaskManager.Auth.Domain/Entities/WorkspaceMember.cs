using TaskManager.Shared.Domain;

namespace TaskManager.Auth.Domain.Entities;

public sealed class WorkspaceMember
{
    private WorkspaceMember()
    {
        // EF Core
    }

    internal WorkspaceMember(Guid workspaceId, Guid userId, WorkspaceRole role)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
        Role = role;
        IsActive = true;
    }

    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public WorkspaceRole Role { get; private set; }
    public bool IsActive { get; private set; }

    public void Deactivate() => IsActive = false;
}
