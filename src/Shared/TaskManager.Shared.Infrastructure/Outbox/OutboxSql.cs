using Microsoft.EntityFrameworkCore;

namespace TaskManager.Shared.Infrastructure.Outbox;

/// <summary>SQL do OutboxProcessor. Schema e tabela vêm do model; valores sempre como parâmetro.</summary>
internal sealed record OutboxSql(string Candidates, string Claim)
{
    public static OutboxSql For(DbContext context, int knownTypeCount)
    {
        var entity = context.Model.FindEntityType(typeof(OutboxMessage))
            ?? throw new InvalidOperationException(
                $"{context.GetType().Name} does not map the Outbox (call modelBuilder.AddOutbox()).");

        var table = $"[{entity.GetSchema()}].[{entity.GetTableName()}]";
        var typeParameters = string.Join(", ", Enumerable.Range(2, knownTypeCount).Select(i => "{" + i + "}"));

        // Sem lock: só escolhe candidatos. Quem garante exclusividade é o Claim.
        var candidates = "SELECT TOP ({0}) [Id] AS [Value] FROM " + table +
            " WHERE [ProcessedAt] IS NULL AND [Attempts] < {1} AND [Type] IN (" + typeParameters + ")" +
            " ORDER BY [OccurredAt]";

        // READPAST: mensagem travada por outra réplica volta vazia, sem esperar.
        var claim = "SELECT * FROM " + table +
            " WITH (UPDLOCK, READPAST, ROWLOCK)" +
            " WHERE [Id] = {0} AND [ProcessedAt] IS NULL AND [Attempts] < {1}";

        return new OutboxSql(candidates, claim);
    }
}
