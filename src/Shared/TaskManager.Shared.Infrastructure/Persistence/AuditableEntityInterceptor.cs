using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TaskManager.Shared.Domain;

namespace TaskManager.Shared.Infrastructure.Persistence;

/// <summary>
/// Preenche CreatedAt/UpdatedAt e incrementa Version. O domínio não
/// precisa saber de relógio para auditoria nem de token de concorrência.
/// </summary>
public sealed class AuditableEntityInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
            return;

        var now = timeProvider.GetUtcNow();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditable)
            {
                if (entry.State == EntityState.Added)
                    entry.Property(nameof(IAuditable.CreatedAt)).CurrentValue = now;
                else if (entry.State == EntityState.Modified)
                    entry.Property(nameof(IAuditable.UpdatedAt)).CurrentValue = now;
            }

            if (entry.Entity is IVersioned versioned && entry.State == EntityState.Modified)
                entry.Property(nameof(IVersioned.Version)).CurrentValue = versioned.Version + 1;
        }
    }
}
