using System.Diagnostics.Metrics;

namespace TaskManager.Tasks.Application.Diagnostics;

/// <summary>
/// Métricas de negócio do módulo. Só BCL (System.Diagnostics.Metrics): quem exporta
/// é o OpenTelemetry da Api. Sem tag de workspace — cardinalidade de tenant explode
/// o custo da métrica; o recorte por workspace é pergunta para log, não para métrica.
/// </summary>
public sealed class TasksMetrics
{
    public const string MeterName = "TaskManager.Tasks";

    private readonly Counter<long> _created;
    private readonly Counter<long> _completed;
    private readonly Histogram<double> _leadTime;

    public TasksMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _created = meter.CreateCounter<long>("taskmanager.tasks.created", "{task}", "Tarefas criadas.");
        _completed = meter.CreateCounter<long>("taskmanager.tasks.completed", "{task}", "Tarefas concluídas (Done).");
        _leadTime = meter.CreateHistogram<double>(
            "taskmanager.tasks.lead_time", "s", "Tempo entre a criação e a conclusão da tarefa.");
    }

    public void TaskCreated(TaskPriority priority) =>
        _created.Add(1, new KeyValuePair<string, object?>("task.priority", priority.ToString()));

    public void TaskCompleted(TaskPriority priority, TimeSpan leadTime)
    {
        var tag = new KeyValuePair<string, object?>("task.priority", priority.ToString());
        _completed.Add(1, tag);
        _leadTime.Record(leadTime.TotalSeconds, tag);
    }
}
