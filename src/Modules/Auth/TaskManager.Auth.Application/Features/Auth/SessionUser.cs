namespace TaskManager.Auth.Application.Features.Auth;

public sealed record SessionUser(Guid Id, string Email, string FullName);
