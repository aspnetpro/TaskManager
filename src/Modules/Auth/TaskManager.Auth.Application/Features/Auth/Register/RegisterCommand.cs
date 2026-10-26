namespace TaskManager.Auth.Application.Features.Auth.Register;

/// <summary>Cria a conta e o primeiro workspace, com o usuário como Admin.</summary>
public sealed record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    string WorkspaceName) : IRequest<Result<AuthResult>>;
