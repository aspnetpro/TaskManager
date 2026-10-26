using FluentValidation;
using MediatR;
using NetArchTest.Rules;
using TaskManager.Shared.Application.Events;

namespace TaskManager.ArchitectureTests;

/// <summary>Convenções de naming da seção 6.4 que valem a pena automatizar, em todos os módulos.</summary>
public sealed class ConventionTests
{
    private static Types Applications => Types.InAssemblies(Assemblies.Applications);

    [Fact]
    public void Handlers_AreSealedAndEndWithHandler()
    {
        var result = Applications
            .That().ImplementInterface(typeof(IRequestHandler<,>))
            .Should().BeSealed()
            .And().HaveNameEndingWith("Handler", StringComparison.Ordinal)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void IntegrationEventHandlers_AreSealedAndEndWithHandler()
    {
        var result = Applications
            .That().ImplementInterface(typeof(IIntegrationEventHandler<>))
            .Should().BeSealed()
            .And().HaveNameEndingWith("Handler", StringComparison.Ordinal)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Validators_AreSealedAndEndWithValidator()
    {
        var result = Applications
            .That().Inherit(typeof(AbstractValidator<>))
            .Should().BeSealed()
            .And().HaveNameEndingWith("Validator", StringComparison.Ordinal)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Theory]
    [InlineData("Tasks")]
    [InlineData("Auth")]
    [InlineData("Notifications")]
    public void CommandsAndQueries_LiveInFeatureFolders(string module)
    {
        var result = Types.InAssemblies(Assemblies.OfModule(module))
            .That().ImplementInterface(typeof(IRequest<>))
            .Should().ResideInNamespaceStartingWith($"TaskManager.{module}.Application.Features")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}
