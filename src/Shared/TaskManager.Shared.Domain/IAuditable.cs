namespace TaskManager.Shared.Domain;

/// <summary>CreatedAt/UpdatedAt preenchidos pelo AuditableEntityInterceptor.</summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; }
    DateTimeOffset? UpdatedAt { get; }
}
