namespace TaskManager.Tasks.Domain.Enums;

/// <summary>
/// Persistido como inteiro: a ordem numérica é a ordem de negócio.
/// Como string, ordenar por prioridade daria Critical, High, Low, Medium.
/// </summary>
public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}
