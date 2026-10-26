namespace TaskManager.Shared.Application;

public static class TimeProviderExtensions
{
    /// <summary>Data de hoje em UTC. Fuso por workspace fica para quando houver requisito.</summary>
    public static DateOnly GetUtcToday(this TimeProvider timeProvider) =>
        DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
