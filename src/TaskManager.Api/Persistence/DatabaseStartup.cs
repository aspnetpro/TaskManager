using Microsoft.EntityFrameworkCore;
using TaskManager.Auth.Application.Abstractions;
using TaskManager.Auth.Infrastructure;
using TaskManager.Auth.Infrastructure.Persistence;
using TaskManager.Notifications.Infrastructure.Persistence;
using TaskManager.Tasks.Infrastructure.Persistence;

namespace TaskManager.Api.Persistence;

public static class DatabaseStartup
{
    /// <summary>
    /// Só em Development: aplica migrations e semeia o workspace demo.
    /// Em staging/prod, migrations rodam no pipeline (Cap. 13), nunca no startup.
    /// </summary>
    public static async Task InitializeDevelopmentDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;

        var auth = services.GetRequiredService<AuthDbContext>();
        await auth.Database.MigrateAsync();
        await services.GetRequiredService<TasksDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();

        var password = app.Configuration[DemoWorkspace.PasswordConfigKey];
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException($"{DemoWorkspace.PasswordConfigKey} is not configured (see appsettings.Development.json).");

        await DemoWorkspace.SeedAsync(auth, services.GetRequiredService<IPasswordHasher>(), password);
    }
}
