using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskManager.Shared.Application;
using TaskManager.Shared.Domain.Results;

namespace TaskManager.Shared.Infrastructure.Persistence;

/// <summary>
/// Implementação base do Unit of Work por DbContext. Converte
/// DbUpdateConcurrencyException e violação de índice único em Result de
/// conflito (409 na Api) — corrida previsível não é 500.
/// </summary>
public abstract class EfUnitOfWork<TContext>(TContext context) : IUnitOfWork
    where TContext : DbContext
{
    // 2601: duplicate key em índice único; 2627: violação de UNIQUE/PRIMARY KEY.
    private static readonly int[] UniqueViolationNumbers = [2601, 2627];

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return CommonErrors.ConcurrencyConflict;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql && UniqueViolationNumbers.Contains(sql.Number))
        {
            return CommonErrors.DuplicateKey;
        }
    }
}
