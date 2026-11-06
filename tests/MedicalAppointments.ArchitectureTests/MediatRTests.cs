using MedicalAppointments.Application.Auth;
using MedicalAppointments.Domain.Identity;
using System.Reflection;

namespace MedicalAppointments.ArchitectureTests;

public class MediatRTests
{
    [Fact]
    public void Domain_And_Application_Should_Not_Reference_MediatR()
    {
        AssertNoMediatR(typeof(User).Assembly);
        AssertNoMediatR(typeof(IAuthService).Assembly);
    }

    private static void AssertNoMediatR(Assembly assembly)
    {
        var references = assembly
            .GetReferencedAssemblies()
            .Select(x => x.Name)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Cast<string>()
            .ToList();

        Assert.DoesNotContain(references, x => x.Contains("MediatR", StringComparison.OrdinalIgnoreCase));
    }
}