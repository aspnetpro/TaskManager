namespace TaskManager.Shared.Domain;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    // Guid v7: ordenável por tempo, evita fragmentação de índice clustered no SQL Server.
    public Guid Id { get; protected init; } = Guid.CreateVersion7();

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
