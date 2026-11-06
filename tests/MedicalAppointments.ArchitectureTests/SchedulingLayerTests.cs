using MedicalAppointments.Application.Scheduling.Availability;
using MedicalAppointments.Domain.Scheduling;
using System.Reflection;

namespace MedicalAppointments.ArchitectureTests;

public class SchedulingLayerTests
{
    private static readonly Assembly DomainAssembly = typeof(DoctorSchedule).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(IAvailabilityService).Assembly;

    [Fact]
    public void Domain_Scheduling_Should_Not_Depend_On_Outer_Layers()
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
    public void Application_Scheduling_Should_Not_Depend_On_Infrastructure_Or_Frameworks()
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