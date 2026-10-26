using System.Reflection;
using NetArchTest.Rules;

namespace TaskManager.ArchitectureTests;

/// <summary>
/// As regras de dependência da seção 5.6, verificadas a cada `dotnet test`.
/// Documentação que o compilador não checa vira sugestão em três sprints.
/// </summary>
public sealed class ModuleBoundaryTests
{
    private static readonly string[] Modules = ["Tasks", "Auth", "Notifications"];

    public static TheoryData<string, Assembly> DomainAssemblies => new()
    {
        { "Shared.Domain", Assemblies.SharedDomain },
        { "Tasks.Domain", Assemblies.TasksDomain },
        { "Auth.Domain", Assemblies.AuthDomain },
        { "Notifications.Domain", Assemblies.NotificationsDomain }
    };

    public static TheoryData<string, Assembly> ApplicationAssemblies => new()
    {
        { "Shared.Application", Assemblies.SharedApplication },
        { "Tasks.Application", Assemblies.TasksApplication },
        { "Auth.Application", Assemblies.AuthApplication },
        { "Notifications.Application", Assemblies.NotificationsApplication }
    };

    public static TheoryData<string, Assembly> ContractsAssemblies => new()
    {
        { "Tasks.Contracts", Assemblies.TasksContracts },
        { "Auth.Contracts", Assemblies.AuthContracts }
    };

    public static TheoryData<string> ModuleNames => new(Modules);

    [Theory]
    [MemberData(nameof(DomainAssemblies))]
    public void Domain_DependsOnNothingButSharedDomain(string name, Assembly assembly)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
            [
                "TaskManager.Shared.Application",
                "TaskManager.Shared.Infrastructure",
                .. Modules.SelectMany(m => Layers(m, "Application", "Infrastructure", "Contracts")),
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "MediatR",
                "FluentValidation"
            ])
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Because(name, result));
    }

    [Theory]
    [MemberData(nameof(ApplicationAssemblies))]
    public void Application_DoesNotDependOnInfrastructureOrWeb(string name, Assembly assembly)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
            [
                "TaskManager.Shared.Infrastructure",
                .. Modules.SelectMany(m => Layers(m, "Infrastructure")),
                "TaskManager.Api",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore"
            ])
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Because(name, result));
    }

    /// <summary>Um módulo só enxerga os outros pelos Contracts — nunca Domain, Application ou Infrastructure.</summary>
    [Theory]
    [MemberData(nameof(ModuleNames))]
    public void Module_TalksToOtherModulesOnlyThroughContracts(string module)
    {
        var forbidden = Modules
            .Where(other => other != module)
            .SelectMany(other => Layers(other, "Domain", "Application", "Infrastructure"))
            .ToArray();

        var result = Types.InAssemblies(Assemblies.OfModule(module))
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Because(module, result));
    }

    [Theory]
    [MemberData(nameof(ContractsAssemblies))]
    public void Contracts_DependOnNoOtherProject(string name, Assembly assembly)
    {
        var self = name.Split('.')[0];

        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
            [
                "TaskManager.Shared",
                .. Modules.Where(m => m != self).Select(m => $"TaskManager.{m}"),
                .. Layers(self, "Domain", "Application", "Infrastructure"),
                "Microsoft.EntityFrameworkCore",
                "MediatR"
            ])
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Because(name, result));
    }

    [Fact]
    public void ApiEndpoints_NeverTouchPersistenceOrEntities()
    {
        var result = Types.InAssembly(Assemblies.Api)
            .That().ResideInNamespace("TaskManager.Api.Endpoints")
            .ShouldNot()
            .HaveDependencyOnAny(
            [
                "Microsoft.EntityFrameworkCore",
                .. Modules.SelectMany(m => Layers(m, "Infrastructure")),
                .. Modules.Select(m => $"TaskManager.{m}.Domain.Entities"),
                "TaskManager.Notifications.Domain.Notification"
            ])
            .GetResult();

        result.IsSuccessful.Should().BeTrue(Because("Api.Endpoints", result));
    }

    private static IEnumerable<string> Layers(string module, params string[] layers) =>
        layers.Select(layer => $"TaskManager.{module}.{layer}");

    private static string Because(string name, TestResult result) =>
        $"{name} violates module boundaries: {string.Join(", ", result.FailingTypeNames ?? [])}";
}
