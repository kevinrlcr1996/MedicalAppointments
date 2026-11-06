using MedicalAppointments.Application.Outbox;
using MedicalAppointments.Domain.Outbox;
using System.Reflection;

namespace MedicalAppointments.ArchitectureTests;

public class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(OutboxMessage).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(OutboxTypes).Assembly;

    [Fact]
    public void Domain_Should_Not_Depend_On_Outer_Layers_Or_Frameworks()
    {
        AssertNoReferences(
            DomainAssembly,
            "MedicalAppointments.Application",
            "MedicalAppointments.Infrastructure",
            "MedicalAppointments.Api",
            "MedicalAppointments.Contracts",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore.App",
            "Microsoft.AspNetCore.SignalR",
            "MediatR");
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Api_Or_Frameworks()
    {
        AssertNoReferences(
            ApplicationAssembly,
            "MedicalAppointments.Infrastructure",
            "MedicalAppointments.Api",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore.App",
            "Microsoft.AspNetCore.SignalR",
            "MediatR");
    }

    private static void AssertNoReferences(Assembly assembly, params string[] forbiddenNames)
    {
        var referenced = assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var broken = referenced
            .Intersect(forbiddenNames, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Empty(broken);
    }
}