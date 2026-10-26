namespace TaskManager.Auth.Application.Features.Members.Add;

/// <summary>Adiciona uma conta existente ao workspace atual. Convite por email fica para depois do MVP.</summary>
public sealed record AddMemberCommand(string Email, WorkspaceRole Role) : IRequest<Result<MemberResponse>>;
