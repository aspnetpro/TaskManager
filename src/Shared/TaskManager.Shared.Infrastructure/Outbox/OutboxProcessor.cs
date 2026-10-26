using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskManager.Shared.Application.Events;

namespace TaskManager.Shared.Infrastructure.Outbox;

/// <summary>
/// Lê o Outbox de um módulo e despacha os eventos. Uma consulta sem lock escolhe o
/// lote; cada mensagem é travada (UPDLOCK + READPAST) e confirmada na sua própria
/// transação. Assim várias réplicas rodam juntas sem processar a mesma mensagem, e
/// uma falha no commit repete uma mensagem — não o lote inteiro (emails incluídos).
/// Só tipos que este processo conhece são lidos: no meio de um deploy, evento criado
/// pela versão nova fica para a réplica nova, em vez de ser dado como processado.
/// Cada mensagem vira um span próprio; o polling vazio não gera telemetria.
/// </summary>
public sealed partial class OutboxProcessor<TContext>(
    IServiceScopeFactory scopeFactory,
    IntegrationEventDispatcher dispatcher,
    IntegrationEventRegistry registry,
    OutboxDiagnostics diagnostics,
    IOptions<OutboxOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxProcessor<TContext>> logger)
    : BackgroundService
    where TContext : DbContext
{
    private static readonly string Module = typeof(TContext).Name.Replace("DbContext", "", StringComparison.Ordinal);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        while (!stoppingToken.IsCancellationRequested)
        {
            var selected = 0;
            try
            {
                selected = await ProcessBatchAsync(settings, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                LogBatchFailed(logger, ex, typeof(TContext).Name);
            }

            // Lote cheio = provavelmente tem mais; não espera o intervalo.
            // Delay em tempo real: o TimeProvider injetado é para "agora", não para agendar.
            if (selected < settings.BatchSize)
                await Task.Delay(settings.PollingInterval, stoppingToken);
        }
    }

    private async Task<int> ProcessBatchAsync(OutboxOptions settings, CancellationToken cancellationToken)
    {
        var knownTypes = registry.Names.ToArray();
        if (knownTypes.Length == 0)
            return 0;

        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var sql = OutboxSql.For(context, knownTypes.Length);

        // {0} = lote, {1} = MaxAttempts, {2..} = tipos conhecidos.
        object[] parameters = [settings.BatchSize, settings.MaxAttempts, .. knownTypes];
        var ids = await context.Database.SqlQueryRaw<Guid>(sql.Candidates, parameters).ToListAsync(cancellationToken);

        foreach (var id in ids)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ProcessOneAsync(context, sql.Claim, id, settings, cancellationToken);
        }

        return ids.Count;
    }

    private async Task ProcessOneAsync(
        TContext context, string claimSql, Guid id, OutboxOptions settings, CancellationToken cancellationToken)
    {
        // EnableRetryOnFailure exige que a transação rode dentro da execution strategy.
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async ct =>
        {
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync(ct);

            // Releitura com lock: outra réplica pode ter pego ou concluído a mensagem desde a seleção.
            var claimed = await context.Set<OutboxMessage>()
                .FromSqlRaw(claimSql, id, settings.MaxAttempts)
                .ToListAsync(ct);
            if (claimed is not [var message])
                return;

            await ProcessAsync(message, settings, ct);

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }, cancellationToken);
    }

    private async Task ProcessAsync(OutboxMessage message, OutboxOptions settings, CancellationToken cancellationToken)
    {
        using var activity = OutboxDiagnostics.ActivitySource.StartActivity($"outbox process {Module}", ActivityKind.Consumer);
        activity?.SetTag("messaging.system", "outbox");
        activity?.SetTag("messaging.message.id", message.Id);
        activity?.SetTag("outbox.message.type", message.Type);
        activity?.SetTag("outbox.message.attempts", message.Attempts);

        try
        {
            var dispatched = await dispatcher.DispatchAsync(message, cancellationToken);
            var now = timeProvider.GetUtcNow();
            message.MarkProcessed(now);
            diagnostics.Processed(Module, now - message.OccurredAt);

            if (!dispatched)
                LogNoConsumer(logger, message.Type, message.Id);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            message.MarkFailed(ex.Message);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            var deadLetter = message.Attempts >= settings.MaxAttempts;
            diagnostics.Failed(Module, deadLetter);

            if (deadLetter)
                LogPoisonMessage(logger, ex, message.Type, message.Id, message.Attempts);
            else
                LogMessageFailed(logger, ex, message.Type, message.Id, message.Attempts);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox batch failed for {Context}")]
    private static partial void LogBatchFailed(ILogger logger, Exception exception, string context);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No consumer for {Type} (message {MessageId})")]
    private static partial void LogNoConsumer(ILogger logger, string type, Guid messageId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox message {Type} {MessageId} failed (attempt {Attempts})")]
    private static partial void LogMessageFailed(ILogger logger, Exception exception, string type, Guid messageId, int attempts);

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox message {Type} {MessageId} gave up after {Attempts} attempts")]
    private static partial void LogPoisonMessage(ILogger logger, Exception exception, string type, Guid messageId, int attempts);
}
