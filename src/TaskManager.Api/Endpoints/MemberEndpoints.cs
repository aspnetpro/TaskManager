using MediatR;
using TaskManager.Api.Http;
using TaskManager.Api.Requests;
using TaskManager.Api.Security;
using TaskManager.Auth.Application.Features.Members;
using TaskManager.Auth.Application.Features.Members.Add;
using TaskManager.Auth.Application.Features.Members.List;

namespace TaskManager.Api.Endpoints;

/// <summary>Membros do workspace do token — não há workspaceId na rota, então não há IDOR para tentar.</summary>
public static class MemberEndpoints
{
    private const string BasePath = "/api/v1/workspace/members";

    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(BasePath)
            .WithTags("Workspace")
            .RequireAuthorization(Policies.WorkspaceMember)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/", ListMembers)
            .WithName("ListMembers")
            .Produces<IReadOnlyList<MemberResponse>>();

        group.MapPost("/", AddMember)
            .WithName("AddMember")
            .RequireAuthorization(Policies.AdminOnly)
            .Produces<MemberResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> ListMembers(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new ListMembersQuery(), ct);
        return result.Match(Results.Ok, error => error.ToProblem());
    }

    private static async Task<IResult> AddMember(AddMemberRequest request, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new AddMemberCommand(request.Email, request.Role), ct);
        return result.Match(
            member => Results.Created($"{BasePath}/{member.UserId}", member),
            error => error.ToProblem());
    }
}
