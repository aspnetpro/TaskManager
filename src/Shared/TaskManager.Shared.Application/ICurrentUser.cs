using TaskManager.Shared.Domain;
using TaskManager.Shared.Domain.Results;

namespace TaskManager.Shared.Application;

/// <summary>Usuário autenticado da requisição atual.</summary>
public interface ICurrentUser
{
    Guid UserId { get; }
    Guid WorkspaceId { get; }
    WorkspaceRole Role { get; }
}
