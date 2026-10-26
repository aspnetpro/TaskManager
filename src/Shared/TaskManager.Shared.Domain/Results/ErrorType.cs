namespace TaskManager.Shared.Domain.Results;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    BusinessRule,
    Unexpected
}
