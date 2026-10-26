namespace TaskManager.Auth.Contracts;

/// <summary>
/// API pública do módulo Auth para outros módulos. Tasks usa isto para
/// validar o assignee (US-006) sem conhecer as entidades do Auth.
/// </summary>
public interface IWorkspaceMembershipService
{
    Task<bool> IsActiveMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);
}
