using TaskManager.Shared.Domain.Results;

namespace TaskManager.Shared.Application;

public static class CommonErrors
{
    public static readonly Error ConcurrencyConflict = Error.Conflict(
        "Concurrency.Conflict",
        "The resource was modified by another request. Reload and try again.");

    public static readonly Error DuplicateKey = Error.Conflict(
        "Persistence.DuplicateKey",
        "A record with the same unique key already exists.");
}
