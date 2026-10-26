using System.Diagnostics;
using System.Reflection;
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;
using TaskManager.Api.Security;
using TaskManager.Shared.Application;
using TaskManager.Shared.Infrastructure.Outbox;
using TaskManager.Tasks.Application.Diagnostics;

namespace TaskManager.Api.Hosting;

/// <summary>
/// Logs (Serilog no console) + traces e métricas (OpenTelemetry). O destino é
/// configuração, não código: OTLP (Aspire Dashboard em dev) e/ou Azure Monitor.
/// Sem nenhum dos dois, a telemetria é coletada e descartada — útil nos testes.
/// </summary>
public static class ObservabilityExtensions
{
    public const string ServiceName = "taskmanager-api";
    public const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";
    public const string AzureMonitorKey = "APPLICATIONINSIGHTS_CONNECTION_STRING";

    public static WebApplicationBuilder AddApiObservability(this WebApplicationBuilder builder)
    {
        var development = builder.Environment.IsDevelopment();

        // Sem os providers padrão (console do .NET): senão cada log sai duas vezes.
        builder.Logging.ClearProviders();

        // writeToProviders: o Serilog também entrega cada log ao provider do OpenTelemetry,
        // que exporta com TraceId/SpanId — log e trace se encontram no mesmo backend.
        builder.Services.AddSerilog(
            (services, logger) =>
            {
                logger.ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", ServiceName);

                if (development)
                    logger.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}");
                else
                    logger.WriteTo.Console(new RenderedCompactJsonFormatter());
            },
            writeToProviders: true);

        var telemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(ServiceName, serviceVersion: ServiceVersion())
                .AddAttributes([new("deployment.environment.name", builder.Environment.EnvironmentName)]))
            .WithTracing(tracing => tracing
                .AddSource(ApplicationDiagnostics.SourceName, OutboxDiagnostics.SourceName)
                .AddAspNetCoreInstrumentation(options => options.Filter = context => !IsHealthCheck(context.Request.Path))
                .AddHttpClientInstrumentation()
                // Query fora de request/mensagem (polling do Outbox a cada 5 s) seria um trace órfão por poll.
                .AddSqlClientInstrumentation(options => options.Filter = _ => HasParentOperation()))
            .WithMetrics(metrics => metrics
                .AddMeter(TasksMetrics.MeterName, OutboxDiagnostics.SourceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter("System.Runtime"))
            // Mensagem formatada no backend (sem ela, o App Insights mostra o template "{RequestPath}...").
            .WithLogging(logging => { }, options =>
            {
                options.IncludeScopes = true;
                options.IncludeFormattedMessage = true;
            });

        if (!string.IsNullOrWhiteSpace(builder.Configuration[OtlpEndpointKey]))
            telemetry.UseOtlpExporter();

        if (builder.Configuration[AzureMonitorKey] is { Length: > 0 } connectionString)
            telemetry.UseAzureMonitorExporter(options => options.ConnectionString = connectionString);

        return builder;
    }

    /// <summary>
    /// Uma linha por request (no lugar dos logs verbosos do ASP.NET) e UserId/WorkspaceId
    /// em todo log e span da request. Ids, nunca email: log não é lugar de dado pessoal.
    /// </summary>
    public static IApplicationBuilder UseApiRequestLogging(this IApplicationBuilder app) =>
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (context, _, exception) =>
                exception is not null || context.Response.StatusCode >= 500 ? Serilog.Events.LogEventLevel.Error
                : IsHealthCheck(context.Request.Path) ? Serilog.Events.LogEventLevel.Verbose
                : Serilog.Events.LogEventLevel.Information;
            options.EnrichDiagnosticContext = (diagnostics, context) =>
            {
                if (WorkspaceClaims.TryGetWorkspace(context.User, out var userId, out var workspaceId, out _))
                {
                    diagnostics.Set("UserId", userId);
                    diagnostics.Set("WorkspaceId", workspaceId);
                }
            };
        });

    /// <summary>Depois da autenticação: só aí o usuário da request é conhecido.</summary>
    public static IApplicationBuilder UseRequestContextEnrichment(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (!WorkspaceClaims.TryGetWorkspace(context.User, out var userId, out var workspaceId, out _))
            {
                await next(context);
                return;
            }

            Activity.Current?.SetTag("user.id", userId);
            Activity.Current?.SetTag("taskmanager.workspace.id", workspaceId);

            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("TaskManager.Api");
            using (logger.BeginScope(new Dictionary<string, object> { ["UserId"] = userId, ["WorkspaceId"] = workspaceId }))
                await next(context);
        });

    private static bool IsHealthCheck(PathString path) => path.StartsWithSegments("/health");

    /// <summary>
    /// No filtro, o span do SQL pode já ser o Activity.Current: o que importa é ter pai.
    /// Span de servidor/consumidor corrente (filtro antes do start) também conta.
    /// </summary>
    private static bool HasParentOperation() =>
        Activity.Current is { } current
        && (current.ParentSpanId != default || current.Kind is ActivityKind.Server or ActivityKind.Consumer);

    private static string ServiceVersion() =>
        typeof(ObservabilityExtensions).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
}
