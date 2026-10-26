using System.Reflection;

namespace TaskManager.ArchitectureTests;

/// <summary>Um tipo âncora por assembly. Projeto novo no solution = linha nova aqui.</summary>
internal static class Assemblies
{
    public static readonly Assembly SharedDomain = typeof(Shared.Domain.Entity).Assembly;
    public static readonly Assembly SharedApplication = typeof(Shared.Application.IUnitOfWork).Assembly;

    public static readonly Assembly TasksDomain = typeof(Tasks.Domain.Entities.TaskItem).Assembly;
    public static readonly Assembly TasksApplication = typeof(Tasks.Application.TasksApplicationModule).Assembly;
    public static readonly Assembly TasksInfrastructure = typeof(Tasks.Infrastructure.TasksModule).Assembly;
    public static readonly Assembly TasksContracts = typeof(Tasks.Contracts.IntegrationEvents.TaskAssignedIntegrationEvent).Assembly;

    public static readonly Assembly AuthDomain = typeof(Auth.Domain.Entities.Workspace).Assembly;
    public static readonly Assembly AuthApplication = typeof(Auth.Application.AuthApplicationModule).Assembly;
    public static readonly Assembly AuthInfrastructure = typeof(Auth.Infrastructure.AuthModule).Assembly;
    public static readonly Assembly AuthContracts = typeof(Auth.Contracts.IWorkspaceMembershipService).Assembly;

    public static readonly Assembly NotificationsDomain = typeof(Notifications.Domain.Notification).Assembly;
    public static readonly Assembly NotificationsApplication = typeof(Notifications.Application.NotificationsApplicationModule).Assembly;
    public static readonly Assembly NotificationsInfrastructure = typeof(Notifications.Infrastructure.NotificationsModule).Assembly;

    public static readonly Assembly Api = typeof(Program).Assembly;

    public static readonly Assembly[] Applications = [TasksApplication, AuthApplication, NotificationsApplication];

    /// <summary>Domain, Application e Infrastructure do módulo (Contracts é a parte pública, fica de fora).</summary>
    public static Assembly[] OfModule(string module) => module switch
    {
        "Tasks" => [TasksDomain, TasksApplication, TasksInfrastructure],
        "Auth" => [AuthDomain, AuthApplication, AuthInfrastructure],
        "Notifications" => [NotificationsDomain, NotificationsApplication, NotificationsInfrastructure],
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, null)
    };
}
