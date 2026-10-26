using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Shared.Application.Events;

namespace TaskManager.Shared.Infrastructure.Outbox;

/// <summary>
/// Entrega in-process: desserializa o evento e chama os handlers registrados,
/// cada mensagem em um scope novo. Quando um módulo sair do processo, esta é a
/// peça trocada por um publisher de Azure Service Bus — o resto não muda.
/// </summary>
public sealed class IntegrationEventDispatcher(IServiceScopeFactory scopeFactory, IntegrationEventRegistry registry)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly MethodInfo DispatchTypedMethod = typeof(IntegrationEventDispatcher)
        .GetMethod(nameof(DispatchTypedAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<bool>>> Invokers = new();

    /// <summary>
    /// False quando o tipo é conhecido mas ninguém neste processo o consome — a mensagem
    /// é dada como processada. Tipo desconhecido é exceção, nunca "sem consumidor":
    /// o processor só lê tipos registrados, então chegar aqui sem registro é bug.
    /// </summary>
    public async Task<bool> DispatchAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (!registry.TryResolve(message.Type, out var eventType))
            throw new InvalidOperationException($"Integration event type '{message.Type}' is not registered.");

        var integrationEvent = JsonSerializer.Deserialize(message.Content, eventType, Json)
            ?? throw new InvalidOperationException($"Outbox message {message.Id} has empty content.");

        var invoker = Invokers.GetOrAdd(eventType, static type =>
            DispatchTypedMethod.MakeGenericMethod(type)
                .CreateDelegate<Func<IServiceProvider, object, CancellationToken, Task<bool>>>());

        await using var scope = scopeFactory.CreateAsyncScope();
        return await invoker(scope.ServiceProvider, integrationEvent, cancellationToken);
    }

    private static async Task<bool> DispatchTypedAsync<TEvent>(
        IServiceProvider services, object integrationEvent, CancellationToken cancellationToken)
        where TEvent : class
    {
        var handled = false;
        foreach (var handler in services.GetServices<IIntegrationEventHandler<TEvent>>())
        {
            await handler.HandleAsync((TEvent)integrationEvent, cancellationToken);
            handled = true;
        }

        return handled;
    }
}
