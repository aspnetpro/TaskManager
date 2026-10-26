namespace TaskManager.Api.Requests;

public sealed record LoginRequest(string Email, string Password, Guid? WorkspaceId);
