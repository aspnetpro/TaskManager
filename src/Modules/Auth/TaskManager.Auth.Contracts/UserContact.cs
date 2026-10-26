namespace TaskManager.Auth.Contracts;

public sealed record UserContact(Guid UserId, string Email, string FullName);
