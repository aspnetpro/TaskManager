using TaskManager.Shared.Domain;

namespace TaskManager.Api.Requests;

public sealed record AddMemberRequest(string Email, WorkspaceRole Role);
