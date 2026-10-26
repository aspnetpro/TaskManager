namespace TaskManager.Auth.Application.Features.Auth.Login;

/// <summary>WorkspaceId opcional: sem ele, entra no workspace mais antigo do usuário.</summary>
public sealed record LoginCommand(string Email, string Password, Guid? WorkspaceId = null) : IRequest<Result<AuthResult>>;
