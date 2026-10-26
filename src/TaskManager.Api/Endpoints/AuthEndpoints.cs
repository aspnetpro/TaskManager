using MediatR;
using TaskManager.Api.Hosting;
using TaskManager.Api.Http;
using TaskManager.Api.Requests;
using TaskManager.Api.Responses;
using TaskManager.Api.Security;
using TaskManager.Auth.Application.Features.Auth;
using TaskManager.Auth.Application.Features.Auth.Login;
using TaskManager.Auth.Application.Features.Auth.Logout;
using TaskManager.Auth.Application.Features.Auth.Refresh;
using TaskManager.Auth.Application.Features.Auth.Register;
using TaskManager.Auth.Domain.Errors;
using TaskManager.Shared.Domain.Results;

namespace TaskManager.Api.Endpoints;

public static class AuthEndpoints
{
    private const string BasePath = "/api/v1/auth";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(BasePath)
            .WithTags("Auth")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingExtensions.AuthPolicy)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithDescription("Creates the account and its first workspace (as Admin). Sets the refresh cookie.")
            .Produces<AuthResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithDescription("Returns an access token for the workspace (oldest one if omitted). Sets the refresh cookie.")
            .Produces<AuthResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/refresh", Refresh)
            .WithName("RefreshSession")
            .WithDescription("Rotates the refresh cookie and returns a new access token.")
            .Produces<AuthResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", Logout)
            .WithName("Logout")
            .WithDescription("Revokes the session behind the refresh cookie and clears it.")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }

    private static async Task<IResult> Register(RegisterRequest request, ISender sender, HttpContext http, CancellationToken ct)
    {
        var result = await sender.Send(
            new RegisterCommand(request.Email, request.Password, request.FullName, request.WorkspaceName), ct);

        return result.Match(
            session => WithCookie(http, session, Results.Created($"{BasePath}/me", AuthResponse.From(session))),
            error => error.ToProblem());
    }

    private static async Task<IResult> Login(LoginRequest request, ISender sender, HttpContext http, CancellationToken ct)
    {
        var result = await sender.Send(new LoginCommand(request.Email, request.Password, request.WorkspaceId), ct);
        return result.Match(
            session => WithCookie(http, session, Results.Ok(AuthResponse.From(session))),
            error => error.ToProblem());
    }

    private static async Task<IResult> Refresh(ISender sender, HttpContext http, CancellationToken ct)
    {
        var refreshToken = RefreshTokenCookie.Read(http.Request);
        var result = string.IsNullOrEmpty(refreshToken)
            ? Result<AuthResult>.Failure(AuthErrors.InvalidRefreshToken)
            : await sender.Send(new RefreshSessionCommand(refreshToken), ct);

        return result.Match(
            session => WithCookie(http, session, Results.Ok(AuthResponse.From(session))),
            error =>
            {
                RefreshTokenCookie.Delete(http.Response);
                return error.ToProblem();
            });
    }

    private static async Task<IResult> Logout(ISender sender, HttpContext http, CancellationToken ct)
    {
        if (RefreshTokenCookie.Read(http.Request) is { Length: > 0 } refreshToken)
            await sender.Send(new LogoutCommand(refreshToken), ct);

        RefreshTokenCookie.Delete(http.Response);
        return Results.NoContent();
    }

    private static IResult WithCookie(HttpContext http, AuthResult session, IResult result)
    {
        RefreshTokenCookie.Append(http.Response, session.RefreshToken, session.RefreshTokenExpiresAt);
        return result;
    }
}
