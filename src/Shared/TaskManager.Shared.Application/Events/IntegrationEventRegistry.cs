namespace TaskManager.Shared.Application.Events;

/// <summary>
/// Tipos de integration event conhecidos, por nome. O Outbox guarda só o nome
/// (nunca o AssemblyQualifiedName) e desserializa apenas tipos registrados aqui.
/// Entram os tipos publicados (AddIntegrationEvent) e os consumidos
/// (AddIntegrationEventHandler). Preenchido só no startup.
/// </summary>
public sealed class IntegrationEventRegistry
{
    private readonly Dictionary<string, Type> _types = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> Names => _types.Keys;

    public void Register(Type eventType) => _types[NameOf(eventType)] = eventType;

    public bool IsRegistered(Type eventType) => _types.ContainsKey(NameOf(eventType));

    public bool TryResolve(string name, out Type eventType) => _types.TryGetValue(name, out eventType!);

    public static string NameOf(Type eventType) => eventType.FullName ?? eventType.Name;
}
