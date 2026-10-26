using TaskManager.Shared.Domain;

namespace TaskManager.Notifications.Domain;

/// <summary>
/// Notificação in-app. Guarda dados, não texto pronto: o frontend monta a
/// frase no idioma do usuário. SourceEventId + RecipientId é único —
/// é isso que torna o consumidor idempotente.
/// </summary>
public sealed class Notification : Entity
{
    public const int MaxTaskTitleLength = 200;
    public const int MaxDetailLength = 50;

    private Notification()
    {
        // EF Core
    }

    public Guid WorkspaceId { get; private set; }
    public Guid RecipientId { get; private set; }
    public NotificationType Type { get; private set; }
    public Guid TaskId { get; private set; }
    public string TaskTitle { get; private set; } = string.Empty;
    public Guid ActorId { get; private set; }

    /// <summary>Complemento por tipo (ex: novo status em TaskStatusChanged).</summary>
    public string? Detail { get; private set; }

    public Guid SourceEventId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    public bool IsRead => ReadAt is not null;

    public static Notification Create(
        Guid workspaceId,
        Guid recipientId,
        NotificationType type,
        Guid taskId,
        string taskTitle,
        Guid actorId,
        string? detail,
        Guid sourceEventId,
        DateTimeOffset createdAt) => new()
        {
            WorkspaceId = workspaceId,
            RecipientId = recipientId,
            Type = type,
            TaskId = taskId,
            TaskTitle = taskTitle.Length > MaxTaskTitleLength ? taskTitle[..MaxTaskTitleLength] : taskTitle,
            ActorId = actorId,
            Detail = detail,
            SourceEventId = sourceEventId,
            CreatedAt = createdAt
        };

    /// <summary>Idempotente: marcar como lida de novo mantém a data original.</summary>
    public void MarkRead(DateTimeOffset now) => ReadAt ??= now;
}
