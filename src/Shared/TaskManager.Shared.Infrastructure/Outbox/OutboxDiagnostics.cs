using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace TaskManager.Shared.Infrastructure.Outbox;

/// <summary>
/// Telemetria do Outbox. O sinal que importa é o lag (ocorrência → despacho):
/// fila crescendo aparece aqui antes de aparecer na reclamação do usuário.
/// </summary>
public sealed class OutboxDiagnostics
{
    public const string SourceName = "TaskManager.Outbox";

    public static readonly ActivitySource ActivitySource = new(SourceName);

    private readonly Counter<long> _messages;
    private readonly Histogram<double> _lag;

    public OutboxDiagnostics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(SourceName);
        _messages = meter.CreateCounter<long>(
            "taskmanager.outbox.messages", "{message}", "Mensagens do Outbox por resultado (processed, failed, dead_letter).");
        _lag = meter.CreateHistogram<double>(
            "taskmanager.outbox.lag", "s", "Tempo entre o evento e o despacho bem-sucedido.");
    }

    public void Processed(string module, TimeSpan lag)
    {
        _messages.Add(1, Tag("outbox.module", module), Tag("outbox.outcome", "processed"));
        _lag.Record(lag.TotalSeconds, Tag("outbox.module", module));
    }

    public void Failed(string module, bool deadLetter) =>
        _messages.Add(1, Tag("outbox.module", module), Tag("outbox.outcome", deadLetter ? "dead_letter" : "failed"));

    private static KeyValuePair<string, object?> Tag(string key, string value) => new(key, value);
}
