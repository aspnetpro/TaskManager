using MediatR;
using TaskManager.Api.Http;
using TaskManager.Api.Requests;
using TaskManager.Api.Security;
using TaskManager.Shared.Application;
using TaskManager.Tasks.Application.Features.Tasks;
using TaskManager.Tasks.Application.Features.Tasks.Assign;
using TaskManager.Tasks.Application.Features.Tasks.Create;
using TaskManager.Tasks.Application.Features.Tasks.Delete;
using TaskManager.Tasks.Application.Features.Tasks.GetById;
using TaskManager.Tasks.Application.Features.Tasks.List;
using TaskManager.Tasks.Application.Features.Tasks.Restore;
using TaskManager.Tasks.Application.Features.Tasks.Transition;
using TaskManager.Tasks.Application.Features.Tasks.Update;

namespace TaskManager.Api.Endpoints;

/// <summary>
/// Endpoints só traduzem HTTP ↔ Command/Query. Nenhuma regra de negócio aqui.
/// </summary>
public static class TaskEndpoints
{
    private const string BasePath = "/api/v1/tasks";

    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(BasePath)
            .WithTags("Tasks")
            .RequireAuthorization(Policies.WorkspaceMember)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPost("/", CreateTask)
            .WithName("CreateTask")
            .RequireAuthorization(Policies.CanWriteTasks)
            .Produces<TaskResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapGet("/", ListTasks)
            .WithName("ListTasks")
            .Produces<PagedResponse<TaskResponse>>()
            .ProducesValidationProblem();

        group.MapGet("/{id:guid}", GetTask)
            .WithName("GetTask")
            .Produces<TaskDetailsResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}", UpdateTask)
            .WithName("UpdateTask")
            .RequireAuthorization(Policies.CanWriteTasks)
            .Produces<TaskResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired);

        group.MapPatch("/{id:guid}/status", TransitionTask)
            .WithName("TransitionTask")
            .RequireAuthorization(Policies.CanWriteTasks)
            .Produces<TaskResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/{id:guid}/assignee", AssignTask)
            .WithName("AssignTask")
            .RequireAuthorization(Policies.CanWriteTasks)
            .Produces<TaskResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapDelete("/{id:guid}", DeleteTask)
            .WithName("DeleteTask")
            .RequireAuthorization(Policies.AdminOnly)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/restore", RestoreTask)
            .WithName("RestoreTask")
            .RequireAuthorization(Policies.AdminOnly)
            .Produces<TaskResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static async Task<IResult> CreateTask(
        CreateTaskRequest request, ISender sender, HttpContext http, CancellationToken ct)
    {
        var result = await sender.Send(
            new CreateTaskCommand(request.Title, request.Description, request.Priority, request.DueDate), ct);

        return result.Match(
            task => WithETag(http, task.Version, Results.Created($"{BasePath}/{task.Id}", task)),
            error => error.ToProblem());
    }

    private static async Task<IResult> ListTasks(
        [AsParameters] ListTasksRequest request, ISender sender, CancellationToken ct)
    {
        var defaults = new GetTasksQuery();
        var query = new GetTasksQuery(
            request.Page ?? defaults.Page,
            request.PageSize ?? defaults.PageSize,
            request.Status,
            request.Priority,
            request.AssigneeId,
            request.Search,
            request.Overdue ?? false,
            request.IncludeDeleted ?? false,
            request.SortBy ?? defaults.SortBy,
            request.SortDirection ?? defaults.SortDirection);

        var result = await sender.Send(query, ct);
        return result.Match(Results.Ok, error => error.ToProblem());
    }

    private static async Task<IResult> GetTask(Guid id, ISender sender, HttpContext http, CancellationToken ct)
    {
        var result = await sender.Send(new GetTaskByIdQuery(id), ct);
        return result.Match(
            task => WithETag(http, task.Version, Results.Ok(task)),
            error => error.ToProblem());
    }

    private static async Task<IResult> UpdateTask(
        Guid id, UpdateTaskRequest request, ISender sender, HttpContext http, CancellationToken ct)
    {
        if (!ETag.TryParse(http.Request.Headers.IfMatch, out var expectedVersion))
            return ETag.PreconditionRequired();

        var result = await sender.Send(new UpdateTaskCommand(
            id,
            expectedVersion,
            request.Title,
            request.Description,
            request.Priority,
            request.DueDate,
            request.ClearDueDate), ct);

        return result.Match(
            task => WithETag(http, task.Version, Results.Ok(task)),
            error => error.ToProblem());
    }

    private static async Task<IResult> TransitionTask(
        Guid id, TransitionTaskRequest request, ISender sender, HttpContext http, CancellationToken ct)
    {
        var result = await sender.Send(new TransitionTaskCommand(id, request.Status), ct);
        return result.Match(
            task => WithETag(http, task.Version, Results.Ok(task)),
            error => error.ToProblem());
    }

    private static async Task<IResult> AssignTask(
        Guid id, AssignTaskRequest request, ISender sender, HttpContext http, CancellationToken ct)
    {
        var result = await sender.Send(new AssignTaskCommand(id, request.AssigneeId), ct);
        return result.Match(
            task => WithETag(http, task.Version, Results.Ok(task)),
            error => error.ToProblem());
    }

    private static async Task<IResult> DeleteTask(Guid id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteTaskCommand(id), ct);
        return result.Match(Results.NoContent, error => error.ToProblem());
    }

    private static async Task<IResult> RestoreTask(Guid id, ISender sender, HttpContext http, CancellationToken ct)
    {
        var result = await sender.Send(new RestoreTaskCommand(id), ct);
        return result.Match(
            task => WithETag(http, task.Version, Results.Ok(task)),
            error => error.ToProblem());
    }

    private static IResult WithETag(HttpContext http, uint version, IResult result)
    {
        http.Response.Headers.ETag = ETag.From(version);
        return result;
    }
}
