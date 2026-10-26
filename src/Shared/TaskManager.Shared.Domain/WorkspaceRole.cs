namespace TaskManager.Shared.Domain;

/// <summary>
/// Papel do usuário no workspace. Fica no Shared Kernel porque todos os
/// módulos precisam dele para autorização, não só o Auth.
/// </summary>
public enum WorkspaceRole
{
    Viewer,
    Developer,
    TechLead,
    Admin
}
