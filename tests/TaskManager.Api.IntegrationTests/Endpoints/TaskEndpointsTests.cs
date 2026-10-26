using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Shared.Domain;
using TaskManager.Tasks.Application.Features.Tasks;
using TaskManager.Tasks.Domain.Enums;
using TaskManager.Api.IntegrationTests.Fixtures;

namespace TaskManager.Api.IntegrationTests.Endpoints;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class TaskEndpointsTests(TaskManagerApiFactory factory)
{
    // ---------- US-001 Criar ----------

    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocationAndETag()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var client = ws.ClientAs(WorkspaceRole.Developer);

        var response = await client.PostTaskAsync(new { title = "  Implement auth  ", priority = "High" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.ETag!.Tag.Should().Be("\"0\"");
        var task = await response.ReadAsync<TaskResponse>();
        response.Headers.Location!.ToString().Should().Be($"{TaskApi.BasePath}/{task.Id}");
        task.Title.Should().Be("Implement auth");
        task.Status.Should().Be(TaskItemStatus.Pending);
        task.ReporterId.Should().Be(ws.UserId(WorkspaceRole.Developer));
    }

    [Fact]
    public async Task Create_InvalidRequest_Returns400WithFieldErrors()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var yesterday = factory.Clock.GetUtcNow().AddDays(-1).ToString("yyyy-MM-dd");

        var response = await ws.ClientAs(WorkspaceRole.Developer)
            .PostTaskAsync(new { title = "", priority = "High", dueDate = yesterday });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadAsync<ValidationProblemDetails>();
        problem.Errors.Keys.Should().BeEquivalentTo("Title", "DueDate");
    }

    [Fact]
    public async Task Create_IntegerEnum_Returns400()
    {
        var ws = await factory.CreateWorkspaceAsync();

        var response = await ws.ClientAs(WorkspaceRole.Developer).PostAsJsonAsync(
            TaskApi.BasePath, new { title = "x", priority = 0 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var response = await factory.CreateClient().PostTaskAsync(new { title = "x", priority = "Low" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_AsViewer_Returns403()
    {
        var ws = await factory.CreateWorkspaceAsync();

        var response = await ws.ClientAs(WorkspaceRole.Viewer).PostTaskAsync(new { title = "x", priority = "Low" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------- Multi-tenancy (IDOR) ----------

    [Fact]
    public async Task Get_TaskFromAnotherWorkspace_Returns404()
    {
        var mine = await factory.CreateWorkspaceAsync();
        var other = await factory.CreateWorkspaceAsync();
        var task = await mine.ClientAs(WorkspaceRole.Developer).CreateTaskAsync(new { title = "Secret", priority = "Low" });

        var response = await other.ClientAs(WorkspaceRole.Admin).GetAsync($"{TaskApi.BasePath}/{task.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_IncludeDeleted_NeverLeaksOtherWorkspaces()
    {
        var mine = await factory.CreateWorkspaceAsync();
        var other = await factory.CreateWorkspaceAsync();
        var task = await mine.ClientAs(WorkspaceRole.Admin).CreateTaskAsync(new { title = "Mine", priority = "Low" });
        await mine.ClientAs(WorkspaceRole.Admin).DeleteAsync($"{TaskApi.BasePath}/{task.Id}");

        var page = await other.ClientAs(WorkspaceRole.Admin).ListAsync("?includeDeleted=true");

        page.TotalCount.Should().Be(0);
    }

    // ---------- US-002 Listar / US-008 Due date ----------

    [Fact]
    public async Task List_SearchesTitleAndDescriptionOnSqlServer()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var client = ws.ClientAs(WorkspaceRole.Developer);
        await client.CreateTaskAsync(new { title = "Configure JWT", priority = "Low" });
        await client.CreateTaskAsync(new { title = "Refactor", description = "move JWT config", priority = "Low" });
        await client.CreateTaskAsync(new { title = "Unrelated", priority = "Low" });

        var page = await client.ListAsync("?search=jwt");

        page.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task List_SortByPriority_UsesBusinessOrderNotAlphabetical()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var client = ws.ClientAs(WorkspaceRole.Developer);
        foreach (var priority in new[] { "Medium", "Critical", "Low", "High" })
            await client.CreateTaskAsync(new { title = priority, priority });

        var page = await client.ListAsync("?sortBy=Priority&sortDirection=Asc");

        page.Items.Select(t => t.Priority).Should().Equal(
            TaskPriority.Low, TaskPriority.Medium, TaskPriority.High, TaskPriority.Critical);
    }

    [Theory]
    [InlineData("CreatedAt")]
    [InlineData("UpdatedAt")]
    [InlineData("Priority")]
    [InlineData("DueDate")]
    public async Task List_EverySortField_TranslatesToSql(string sortBy)
    {
        var ws = await factory.CreateWorkspaceAsync();
        var client = ws.ClientAs(WorkspaceRole.Developer);
        await client.CreateTaskAsync(new { title = "x", priority = "Low" });

        var asc = await client.GetAsync($"{TaskApi.BasePath}?sortBy={sortBy}&sortDirection=Asc");
        var desc = await client.GetAsync($"{TaskApi.BasePath}?sortBy={sortBy}&sortDirection=Desc");

        asc.StatusCode.Should().Be(HttpStatusCode.OK);
        desc.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("Asc")]
    [InlineData("Desc")]
    public async Task List_SortByDueDate_PutsTasksWithoutDueDateLast(string direction)
    {
        var ws = await factory.CreateWorkspaceAsync();
        var client = ws.ClientAs(WorkspaceRole.Developer);
        var today = factory.Clock.GetUtcNow();
        await client.CreateTaskAsync(new { title = "No date", priority = "Low" });
        await client.CreateTaskAsync(new { title = "Later", priority = "Low", dueDate = today.AddDays(5).ToString("yyyy-MM-dd") });
        await client.CreateTaskAsync(new { title = "Sooner", priority = "Low", dueDate = today.AddDays(1).ToString("yyyy-MM-dd") });

        var page = await client.ListAsync($"?sortBy=DueDate&sortDirection={direction}");

        var expected = direction == "Asc" ? new[] { "Sooner", "Later", "No date" } : ["Later", "Sooner", "No date"];
        page.Items.Select(t => t.Title).Should().Equal(expected);
    }

    [Fact]
    public async Task List_FiltersAndPaginates()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var client = ws.ClientAs(WorkspaceRole.Developer);
        for (var i = 0; i < 5; i++)
            await client.CreateTaskAsync(new { title = $"High {i}", priority = "High" });
        await client.CreateTaskAsync(new { title = "Low", priority = "Low" });

        var page = await client.ListAsync("?priority=High&page=2&pageSize=2");

        page.TotalCount.Should().Be(5);
        page.TotalPages.Should().Be(3);
        page.Items.Should().HaveCount(2).And.OnlyContain(t => t.Priority == TaskPriority.High);
    }

    [Fact]
    public async Task List_Overdue_ReturnsOnlyOpenTasksPastDueDate()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var client = ws.ClientAs(WorkspaceRole.Developer);
        var tomorrow = factory.Clock.GetUtcNow().AddDays(1).ToString("yyyy-MM-dd");
        var open = await client.CreateTaskAsync(new { title = "Open", priority = "Low", dueDate = tomorrow });
        var done = await client.CreateTaskAsync(new { title = "Done", priority = "Low", dueDate = tomorrow });
        await client.PatchJsonAsync($"{TaskApi.BasePath}/{done.Id}/status", new { status = "InProgress" });
        await client.PatchJsonAsync($"{TaskApi.BasePath}/{done.Id}/status", new { status = "Done" });

        // O relógio só anda para frente; os demais testes usam datas relativas a ele.
        factory.Clock.Advance(TimeSpan.FromDays(3));

        var page = await client.ListAsync("?overdue=true");

        page.Items.Should().ContainSingle().Which.Id.Should().Be(open.Id);
        page.Items[0].IsOverdue.Should().BeTrue();
    }

    // ---------- US-003 Detalhe ----------

    [Fact]
    public async Task Get_ReturnsStatusHistoryAndETag()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var client = ws.ClientAs(WorkspaceRole.Developer);
        var task = await client.CreateTaskAsync(new { title = "Ship it", priority = "High" });
        await client.PatchJsonAsync($"{TaskApi.BasePath}/{task.Id}/status", new { status = "InProgress" });

        var response = await client.GetAsync($"{TaskApi.BasePath}/{task.Id}");

        response.Headers.ETag!.Tag.Should().Be("\"1\"");
        var details = await response.ReadAsync<TaskDetailsResponse>();
        details.StatusHistory.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new
            {
                From = TaskItemStatus.Pending,
                To = TaskItemStatus.InProgress,
                ChangedBy = ws.UserId(WorkspaceRole.Developer)
            });
    }

    // ---------- US-004 Editar ----------

    [Fact]
    public async Task Patch_WithoutIfMatch_Returns428()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var client = ws.ClientAs(WorkspaceRole.Developer);
        var task = await client.CreateTaskAsync(new { title = "x", priority = "Low" });

        var response = await client.PatchJsonAsync($"{TaskApi.BasePath}/{task.Id}", new { title = "y" });

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionRequired);
    }

    [Fact]
    public async Task Patch_ValidIfMatch_UpdatesAndReturnsNewETag()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var client = ws.ClientAs(WorkspaceRole.Developer);
        var task = await client.CreateTaskAsync(new { title = "x", priority = "Low" });

        var response = await client.PatchJsonAsync($"{TaskApi.BasePath}/{task.Id}", new { title = "Renamed" }, "\"0\"");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag!.Tag.Should().Be("\"1\"");
        (await response.ReadAsync<TaskResponse>()).Title.Should().Be("Renamed");
    }

    [Fact]
    public async Task Patch_StaleIfMatch_Returns409()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var client = ws.ClientAs(WorkspaceRole.Developer);
        var task = await client.CreateTaskAsync(new { title = "x", priority = "Low" });
        await client.PatchJsonAsync($"{TaskApi.BasePath}/{task.Id}", new { title = "First" }, "\"0\"");

        var response = await client.PatchJsonAsync($"{TaskApi.BasePath}/{task.Id}", new { title = "Second" }, "\"0\"");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Patch_ByUserWhoIsNotReporterNorAssignee_Returns403()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var task = await ws.ClientAs(WorkspaceRole.Developer).CreateTaskAsync(new { title = "x", priority = "Low" });

        var response = await ws.ClientAs(WorkspaceRole.TechLead)
            .PatchJsonAsync($"{TaskApi.BasePath}/{task.Id}", new { title = "y" }, "\"0\"");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------- US-005 Transição ----------

    [Fact]
    public async Task Transition_Invalid_Returns422WithCode()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var client = ws.ClientAs(WorkspaceRole.Developer);
        var task = await client.CreateTaskAsync(new { title = "x", priority = "Low" });

        var response = await client.PatchJsonAsync($"{TaskApi.BasePath}/{task.Id}/status", new { status = "Done" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.ReadAsync<ProblemDetails>();
        problem.Extensions["code"]!.ToString().Should().Be("Task.InvalidTransition");
    }

    [Fact]
    public async Task Transition_StartUnassignedThenComplete_AutoAssignsAndSetsCompletedAt()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var client = ws.ClientAs(WorkspaceRole.Developer);
        var task = await client.CreateTaskAsync(new { title = "x", priority = "Low" });

        var started = await (await client.PatchJsonAsync($"{TaskApi.BasePath}/{task.Id}/status", new { status = "InProgress" }))
            .ReadAsync<TaskResponse>();
        var done = await (await client.PatchJsonAsync($"{TaskApi.BasePath}/{task.Id}/status", new { status = "Done" }))
            .ReadAsync<TaskResponse>();

        started.AssigneeId.Should().Be(ws.UserId(WorkspaceRole.Developer));
        done.CompletedAt.Should().Be(factory.Clock.GetUtcNow());
    }

    // ---------- US-006 Atribuir ----------

    [Fact]
    public async Task Assign_ToNonMember_Returns400()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var lead = ws.ClientAs(WorkspaceRole.TechLead);
        var task = await lead.CreateTaskAsync(new { title = "x", priority = "Low" });

        var response = await lead.PutAsJsonAsync($"{TaskApi.BasePath}/{task.Id}/assignee", new { assigneeId = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Assign_MemberOfAnotherWorkspace_Returns400()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var other = await factory.CreateWorkspaceAsync();
        var lead = ws.ClientAs(WorkspaceRole.TechLead);
        var task = await lead.CreateTaskAsync(new { title = "x", priority = "Low" });

        var response = await lead.PutAsJsonAsync(
            $"{TaskApi.BasePath}/{task.Id}/assignee", new { assigneeId = other.UserId(WorkspaceRole.Developer) });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Assign_LeadAssignsDeveloper_Returns200()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var lead = ws.ClientAs(WorkspaceRole.TechLead);
        var task = await lead.CreateTaskAsync(new { title = "x", priority = "Low" });

        var response = await lead.PutAsJsonAsync(
            $"{TaskApi.BasePath}/{task.Id}/assignee", new { assigneeId = ws.UserId(WorkspaceRole.Developer) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.ReadAsync<TaskResponse>()).AssigneeId.Should().Be(ws.UserId(WorkspaceRole.Developer));
    }

    [Fact]
    public async Task Assign_DeveloperAssignsSomeoneElse_Returns403()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var dev = ws.ClientAs(WorkspaceRole.Developer);
        var task = await dev.CreateTaskAsync(new { title = "x", priority = "Low" });

        var response = await dev.PutAsJsonAsync(
            $"{TaskApi.BasePath}/{task.Id}/assignee", new { assigneeId = ws.UserId(WorkspaceRole.TechLead) });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------- US-007 Soft delete ----------

    [Fact]
    public async Task Delete_AsDeveloper_Returns403()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var dev = ws.ClientAs(WorkspaceRole.Developer);
        var task = await dev.CreateTaskAsync(new { title = "x", priority = "Low" });

        var response = await dev.DeleteAsync($"{TaskApi.BasePath}/{task.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_AsAdmin_HidesTaskUntilRestored()
    {
        var ws = await factory.CreateWorkspaceAsync();
        var admin = ws.ClientAs(WorkspaceRole.Admin);
        var task = await admin.CreateTaskAsync(new { title = "Obsolete", priority = "Low" });

        (await admin.DeleteAsync($"{TaskApi.BasePath}/{task.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.GetAsync($"{TaskApi.BasePath}/{task.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.ListAsync()).TotalCount.Should().Be(0);
        (await admin.ListAsync("?includeDeleted=true")).Items.Should().ContainSingle(t => t.IsDeleted);
        (await ws.ClientAs(WorkspaceRole.Developer).GetAsync($"{TaskApi.BasePath}?includeDeleted=true"))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var restored = await admin.PostAsync($"{TaskApi.BasePath}/{task.Id}/restore", null);

        restored.StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.GetAsync($"{TaskApi.BasePath}/{task.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
