using TaskManager.Shared.Domain.Results;

namespace TaskManager.Auth.Domain.Errors;

public static class AuthErrors
{
    // Mesma mensagem para email inexistente e senha errada: não revela quais emails têm conta.
    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "Auth.InvalidCredentials", "Invalid email or password.");

    public static readonly Error InvalidRefreshToken = Error.Unauthorized(
        "Auth.InvalidRefreshToken", "The session has expired. Sign in again.");

    public static readonly Error EmailAlreadyRegistered = Error.Conflict(
        "Auth.EmailAlreadyRegistered", "An account with this email already exists.");

    public static readonly Error NoWorkspaceAccess = Error.Forbidden(
        "Auth.NoWorkspaceAccess", "The user is not an active member of the requested workspace.");

    public static readonly Error AlreadyMember = Error.Conflict(
        "Auth.AlreadyMember", "The user is already a member of this workspace.");

    public static Error UserNotFound(string email) => Error.NotFound(
        "Auth.UserNotFound", $"No account found for '{email}'.");
}
