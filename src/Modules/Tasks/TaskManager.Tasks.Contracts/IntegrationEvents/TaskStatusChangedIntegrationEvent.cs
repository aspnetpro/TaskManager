namespace TaskManager.Tasks.Contracts.IntegrationEvents;

/// <summary>
/// Publicado via Outbox a cada transição de status. Status vai como string:
/// Contracts não referencia o Domain, então o enum não atravessa a fronteira.
/// </summary>
public sealed record TaskStatusChangedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid WorkspaceId,
    Guid TaskId,
    string Title,
    string FromStatus,
    string ToStatus,
    Guid ChangedBy,
    Guid ReporterId,
    Guid? AssigneeId);
