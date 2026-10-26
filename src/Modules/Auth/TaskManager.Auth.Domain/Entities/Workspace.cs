using TaskManager.Auth.Domain.Errors;
using TaskManager.Shared.Domain;
using TaskManager.Shared.Domain.Results;

namespace TaskManager.Auth.Domain.Entities;

public sealed class Workspace : Entity, IAuditable
{
    public const int MaxNameLength = 100;

    private readonly List<WorkspaceMember> _members = [];

    private Workspace()
    {
        // EF Core
    }

    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyList<WorkspaceMember> Members => _members;

    public static Workspace Create(string name, Guid? id = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Workspace { Id = id ?? Guid.CreateVersion7(), Name = name.Trim() };
    }

    public Result<WorkspaceMember> AddMember(Guid userId, WorkspaceRole role)
    {
        if (_members.Any(m => m.UserId == userId))
            return AuthErrors.AlreadyMember;

        var member = new WorkspaceMember(Id, userId, role);
        _members.Add(member);
        return member;
    }
}
