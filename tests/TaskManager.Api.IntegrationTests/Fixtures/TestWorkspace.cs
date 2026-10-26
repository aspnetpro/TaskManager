using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;
using TaskManager.Auth.Domain.Entities;
using TaskManager.Auth.Infrastructure.Persistence;
using TaskManager.Shared.Domain;
using TaskManager.Tasks.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace TaskManager.Api.IntegrationTests.Fixtures;

public sealed record TestWorkspace(TaskManagerApiFactory Factory, Guid Id, IReadOnlyDictionary<WorkspaceRole, Guid> Members)
{
    public Guid UserId(WorkspaceRole role) => Members[role];

    public HttpClient ClientAs(WorkspaceRole role) => Factory.CreateClientFor(Members[role], Id, role);
}
