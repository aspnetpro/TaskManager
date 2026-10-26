namespace TaskManager.Auth.Application.Abstractions;

public enum PasswordVerification
{
    Failed,
    Success,
    SuccessRehashNeeded
}
