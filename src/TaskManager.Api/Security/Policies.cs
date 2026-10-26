using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using TaskManager.Shared.Application;
using TaskManager.Shared.Domain;

namespace TaskManager.Api.Security;

public static class Policies
{
    public const string WorkspaceMember = nameof(WorkspaceMember);
    public const string CanWriteTasks = nameof(CanWriteTasks);
    public const string AdminOnly = nameof(AdminOnly);
}
