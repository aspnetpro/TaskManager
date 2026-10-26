using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using TaskManager.Auth.Domain.Entities;
using TaskManager.Auth.Infrastructure.Persistence;
using TaskManager.Notifications.Infrastructure.Persistence;
using TaskManager.Shared.Domain;
using TaskManager.Tasks.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace TaskManager.Api.IntegrationTests.Fixtures;

/// <summary>
/// Sobe um SQL Server real (Testcontainers) uma vez por execução, aplica as
/// migrations e hospeda a Api com relógio controlável. Request com os headers
/// X-Test-* usa o TestAuthHandler; sem eles, vale o JWT real emitido pelo Auth.
/// </summary>
public sealed class TaskManagerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestOrBearerScheme = "TestOrBearer";

    private readonly MsSqlContainer _sql = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public FakeTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero));

    public string ConnectionString => _sql.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Database", ConnectionString);
        builder.UseSetting("Jwt:SigningKey", "integration-tests-signing-key-not-a-real-secret-0123456789");
        builder.UseSetting("Outbox:PollingInterval", "00:00:00.100");
        // Todos os clientes de teste compartilham o mesmo "IP": limites altos, exceto no teste de 429.
        builder.UseSetting("RateLimiting:GlobalPermitLimit", "100000");
        builder.UseSetting("RateLimiting:AuthPermitLimit", "100000");

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null)
                .AddPolicyScheme(TestOrBearerScheme, null, options => options.ForwardDefaultSelector = context =>
                    context.Request.Headers.ContainsKey(TestAuthHandler.UserHeader)
                        ? TestAuthHandler.SchemeName
                        : JwtBearerDefaults.AuthenticationScheme);

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestOrBearerScheme;
                options.DefaultAuthenticateScheme = TestOrBearerScheme;
                options.DefaultChallengeScheme = TestOrBearerScheme;
            });

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();

        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<TasksDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<NotificationsDbContext>().Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _sql.DisposeAsync();
    }

    /// <summary>Workspace novo por teste: o filtro de tenant isola os dados entre testes.</summary>
    public async Task<TestWorkspace> CreateWorkspaceAsync()
    {
        var workspace = Workspace.Create($"ws-{Guid.NewGuid():N}");
        var members = Enum.GetValues<WorkspaceRole>().ToDictionary(r => r, _ => Guid.CreateVersion7());
        foreach (var (role, userId) in members)
            workspace.AddMember(userId, role);

        await using var scope = Services.CreateAsyncScope();
        var auth = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        auth.Workspaces.Add(workspace);
        await auth.SaveChangesAsync();

        return new TestWorkspace(this, workspace.Id, members);
    }

    public HttpClient CreateClientFor(Guid userId, Guid workspaceId, WorkspaceRole role)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserHeader, userId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.WorkspaceHeader, workspaceId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role.ToString());
        return client;
    }
}
