using System.Text.Json.Serialization;
using Azure.Identity;
using Scalar.AspNetCore;
using TaskManager.Api.Endpoints;
using TaskManager.Api.Hosting;
using TaskManager.Api.Persistence;
using TaskManager.Api.Security;
using TaskManager.Auth.Infrastructure;
using TaskManager.Notifications.Infrastructure;
using TaskManager.Shared.Application;
using TaskManager.Shared.Infrastructure;
using TaskManager.Tasks.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Staging/prod: secrets do Key Vault (Jwt--SigningKey, ConnectionStrings--Database...).
// Em dev a variável fica vazia e valem os user-secrets.
if (builder.Configuration["KeyVault:VaultUri"] is { Length: > 0 } vaultUri)
    builder.Configuration.AddAzureKeyVault(new Uri(vaultUri), new DefaultAzureCredential());

builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = 1_048_576);
builder.AddApiObservability();

// Shared
builder.Services.AddSharedApplication();
builder.Services.AddSharedInfrastructure(builder.Configuration);

// Modules
builder.Services.AddTasksModule(builder.Configuration);
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddNotificationsModule(builder.Configuration);

// Api
builder.Services.AddApiForwardedHeaders(builder.Configuration);
builder.Services.AddApiSecurity();
builder.Services.AddApiRateLimiting(builder.Configuration);
builder.Services.AddApiCors(builder.Configuration);
builder.Services.AddApiHealthChecks(builder.Configuration);
builder.Services.AddApiOpenApi();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

var app = builder.Build();

// Primeiro: o IP real do cliente vale para log, rate limit e tudo que vem depois.
app.UseForwardedHeaders();
app.UseApiRequestLogging();

// BadHttpRequestException (JSON inválido, enum desconhecido, body > 1 MB) mantém o próprio status.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = ex => ex is BadHttpRequestException bad
        ? bad.StatusCode
        : StatusCodes.Status500InternalServerError
});
app.UseStatusCodePages();
app.UseSecurityHeaders();
if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseCors();
app.UseAuthentication();
app.UseRequestContextEnrichment();
// Depois da autenticação: o limite global particiona por usuário quando há token.
app.UseRateLimiter();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(options => options.WithTitle("TaskManager API")).AllowAnonymous();
    await app.InitializeDevelopmentDatabaseAsync();
}

app.MapAuthEndpoints();
app.MapMemberEndpoints();
app.MapTaskEndpoints();
app.MapNotificationEndpoints();
app.MapApiHealthChecks();

await app.RunAsync();

// Exposto para WebApplicationFactory<Program> nos testes de integração.
public partial class Program;
