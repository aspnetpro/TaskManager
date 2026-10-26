namespace TaskManager.Auth.Application.Features.Members;

public sealed record MemberResponse(Guid UserId, string Email, string FullName, WorkspaceRole Role, bool IsActive);
