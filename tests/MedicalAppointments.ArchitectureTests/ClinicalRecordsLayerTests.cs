using System.Reflection;
using MedicalAppointments.Application.ClinicalRecords;
using MedicalAppointments.Domain.ClinicalRecords;
using Xunit;

namespace MedicalAppointments.ArchitectureTests;

public class ClinicalRecordsLayerTests
{
    private static readonly Assembly DomainAssembly = typeof(ClinicalNote).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(IClinicalRecordsService).Assembly;

    [Fact]
    public void Domain_ClinicalRecords_Should_Not_Depend_On_Outer_Layers()
    {
        AssertNoReferences(DomainAssembly,
            "MedicalAppointments.Application", "MedicalAppointments.Infrastructure",
            "MedicalAppointments.Api", "MedicalAppointments.Contracts",
            "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore.App", "MediatR");
    }

    [Fact]
    public void Application_ClinicalRecords_Should_Not_Depend_On_Infrastructure_Or_Frameworks()
    {
        AssertNoReferences(ApplicationAssembly,
            "MedicalAppointments.Infrastructure", "MedicalAppointments.Api",
            "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore.App", "MediatR");
    }

    private static void AssertNoReferences(Assembly assembly, params string[] forbiddenNames)
    {
        var referenced = assembly.GetReferencedAssemblies()
            .Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.Empty(referenced.Intersect(forbiddenNames, StringComparer.OrdinalIgnoreCase).ToArray());
    }
}