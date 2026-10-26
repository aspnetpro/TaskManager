namespace TaskManager.Tasks.Contracts.IntegrationEvents;

/// <summary>
/// Publicado via Outbox quando o responsável de uma tarefa muda. Carrega o
/// que o consumidor precisa (título, workspace) para não ter que chamar Tasks de volta.
/// EventId é a chave de idempotência: a entrega é at-least-once.
/// </summary>
public sealed record TaskAssignedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid WorkspaceId,
    Guid TaskId,
    string Title,
    Guid? PreviousAssigneeId,
    Guid? AssigneeId,
    Guid AssignedBy);
