using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaskManager.Shared.Application.Events;
using TaskManager.Shared.Domain;

namespace TaskManager.Shared.Infrastructure.Outbox;

/// <summary>
/// Converte domain events em integration events e grava como OutboxMessage
/// no mesmo SaveChanges. Banco salvou = evento salvo; banco falhou = nada publicado.
/// Cada módulo decide o que vira integration event (domain event é detalhe interno).
/// </summary>
public abstract class OutboxInterceptor(TimeProvider timeProvider, IntegrationEventRegistry registry) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>Devolve o integration event correspondente, ou null se o evento é só interno.</summary>
    protected abstract object? ToIntegrationEvent(Entity source, IDomainEvent domainEvent, DateTimeOffset occurredAt);

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Capture(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Capture(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Capture(DbContext? context)
    {
        if (context is null)
            return;

        var now = timeProvider.GetUtcNow();
        var entities = context.ChangeTracker.Entries<Entity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        foreach (var entity in entities)
        {
            foreach (var domainEvent in entity.DomainEvents)
            {
                if (ToIntegrationEvent(entity, domainEvent, now) is not { } integrationEvent)
                    continue;

                // Tipo fora do registry nunca seria lido pelo processor: falha aqui, no primeiro teste.
                var type = integrationEvent.GetType();
                if (!registry.IsRegistered(type))
                    throw new InvalidOperationException(
                        $"{type.Name} is not registered. Call services.AddIntegrationEvent<{type.Name}>() in the publishing module.");

                context.Set<OutboxMessage>().Add(new OutboxMessage(
                    IntegrationEventRegistry.NameOf(type),
                    JsonSerializer.Serialize(integrationEvent, type, Json),
                    now));
            }

            entity.ClearDomainEvents();
        }
    }
}
