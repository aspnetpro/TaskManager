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

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<TaskManagerApiFactory>
{
    public const string Name = "Api";
}
