namespace TaskManager.Auth.Application.Abstractions;

public interface IWorkspaceRepository
{
    void Add(Workspace workspace);

    Task<Workspace?> GetWithMembersAsync(Guid workspaceId, CancellationToken cancellationToken = default);

    /// <summary>Workspaces em que o usuário é membro ativo, do mais antigo para o mais novo.</summary>
    Task<IReadOnlyList<MembershipSummary>> GetActiveMembershipsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MemberResponse>> ListMembersAsync(Guid workspaceId, CancellationToken cancellationToken = default);
}
