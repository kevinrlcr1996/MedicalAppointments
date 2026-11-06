using System.Reflection;
using MedicalAppointments.Application.MedicalStructure.Specialties;
using MedicalAppointments.Domain.MedicalStructure;
using Xunit;

namespace MedicalAppointments.ArchitectureTests;

public class MedicalStructureLayerTests
{
    private static readonly Assembly DomainAssembly = typeof(Specialty).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ISpecialtyService).Assembly;

    [Fact]
    public void Domain_MedicalStructure_Should_Not_Depend_On_Outer_Layers()
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
    public void Application_MedicalStructure_Should_Not_Depend_On_Infrastructure_Or_Frameworks()
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