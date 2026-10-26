using System.ComponentModel.DataAnnotations;

namespace TaskManager.Shared.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    [Range(typeof(TimeSpan), "00:00:00.050", "00:10:00")]
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    [Range(1, 500)]
    public int BatchSize { get; set; } = 20;

    /// <summary>Depois disso a mensagem fica parada para análise (poison message).</summary>
    [Range(1, 100)]
    public int MaxAttempts { get; set; } = 5;
}
