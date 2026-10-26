using System.ComponentModel.DataAnnotations;

namespace TaskManager.Api.Hosting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Por usuário autenticado (ou por IP, se anônimo), por janela.</summary>
    [Range(1, 100_000)]
    public int GlobalPermitLimit { get; set; } = 100;

    /// <summary>Por IP nos endpoints de auth: o freio contra brute force de senha.</summary>
    [Range(1, 100_000)]
    public int AuthPermitLimit { get; set; } = 10;

    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
