using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskManager.Api.Security;

namespace TaskManager.Api.IntegrationTests.Fixtures;

/// <summary>
/// Substitui o JWT nos testes: as claims vêm de headers. Sem header = anônimo (401).
/// Existe só no projeto de testes, nunca no código de produção.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string UserHeader = "X-Test-User";
    public const string WorkspaceHeader = "X-Test-Workspace";
    public const string RoleHeader = "X-Test-Role";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserHeader, out var user))
            return Task.FromResult(AuthenticateResult.NoResult());

        var identity = new ClaimsIdentity(
        [
            new Claim(WorkspaceClaims.UserId, user.ToString()),
            new Claim(WorkspaceClaims.WorkspaceId, Request.Headers[WorkspaceHeader].ToString()),
            new Claim(WorkspaceClaims.WorkspaceRole, Request.Headers[RoleHeader].ToString())
        ], SchemeName);

        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
