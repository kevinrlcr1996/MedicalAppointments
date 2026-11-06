using System.Security.Claims;

namespace MedicalAppointments.Application.Common;

public interface ICurrentPrincipalProvider
{
    ClaimsPrincipal? GetCurrentPrincipal();
}