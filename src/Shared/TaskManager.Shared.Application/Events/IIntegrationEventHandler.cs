namespace TaskManager.Shared.Application.Events;

/// <summary>
/// Consumidor de um integration event publicado por outro módulo via Outbox.
/// Entrega é at-least-once: o handler precisa ser idempotente.
/// </summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : class
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
