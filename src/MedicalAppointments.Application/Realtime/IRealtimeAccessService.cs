using System.Security.Claims;

namespace MedicalAppointments.Application.Realtime;

public interface IRealtimeAccessService
{
    Task<bool> CanJoinDoctorGroupAsync(
        ClaimsPrincipal principal,
        Guid doctorId,
        CancellationToken cancellationToken = default);

    Task<bool> CanJoinClinicGroupAsync(
        ClaimsPrincipal principal,
        Guid clinicId,
        CancellationToken cancellationToken = default);
}