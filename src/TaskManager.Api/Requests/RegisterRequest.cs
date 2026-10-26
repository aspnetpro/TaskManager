namespace TaskManager.Api.Requests;

public sealed record RegisterRequest(string Email, string Password, string FullName, string WorkspaceName);
