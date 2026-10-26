namespace TaskManager.Tasks.Application.Features.Tasks;

/// <summary>
/// Chaves de cache do módulo. Sem workspace aqui de propósito: o
/// IWorkspaceCache aplica o prefixo do tenant em toda chave.
/// </summary>
public static class TaskCacheKeys
{
    public static string Details(Guid taskId) => $"tasks:details:{taskId:N}";
}
