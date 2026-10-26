using System.ComponentModel.DataAnnotations;

namespace TaskManager.Shared.Infrastructure.Caching;

public sealed class CachingOptions
{
    public const string SectionName = "Caching";

    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// L1 em memória — só usado sem Redis (uma instância). Com Redis o L1 fica
    /// desligado: RemoveAsync não alcança o L1 das outras réplicas.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan LocalExpiration { get; set; } = TimeSpan.FromSeconds(30);
}
