namespace TaskManager.Shared.Infrastructure.Outbox;

/// <summary>
/// Integration event gravado na mesma transação da mudança que o originou.
/// Cada módulo tem a sua tabela, no seu schema, junto com os seus dados.
/// </summary>
public sealed class OutboxMessage
{
    public const int MaxTypeLength = 300;
    public const int MaxErrorLength = 2000;

    private OutboxMessage()
    {
        // EF Core
    }

    public OutboxMessage(string type, string content, DateTimeOffset occurredAt)
    {
        Type = type;
        Content = content;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string Type { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? Error { get; private set; }

    public void MarkProcessed(DateTimeOffset now)
    {
        ProcessedAt = now;
        Error = null;
    }

    public void MarkFailed(string error)
    {
        Attempts++;
        Error = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
    }
}
