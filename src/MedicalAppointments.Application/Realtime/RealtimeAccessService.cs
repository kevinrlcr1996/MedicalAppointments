using System.Security.Claims;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.MedicalStructure.Clinics;
using MedicalAppointments.Application.MedicalStructure.Doctors;

namespace MedicalAppointments.Application.Realtime;

public sealed class RealtimeAccessService : IRealtimeAccessService
{
    private readonly IDoctorStore _doctorStore;
    private readonly IClinicStore _clinicStore;

    public RealtimeAccessService(
        IDoctorStore doctorStore,
        IClinicStore clinicStore)
    {
        _doctorStore = doctorStore;
        _clinicStore = clinicStore;
    }

    public async Task<bool> CanJoinDoctorGroupAsync(
        ClaimsPrincipal principal,
        Guid doctorId,
        CancellationToken cancellationToken = default)
    {
        if (doctorId == Guid.Empty)
        {
            return false;
        }

        if (principal.HasPermission(Permissions.SchedulesRead) ||
            principal.HasPermission(Permissions.SchedulesManage))
        {
            return true;
        }

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return false;
        }

        var doctorUserId = await _doctorStore.GetUserIdAsync(doctorId, cancellationToken);

        return doctorUserId == parsedUserId;
    }

    public async Task<bool> CanJoinClinicGroupAsync(
        ClaimsPrincipal principal,
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        if (clinicId == Guid.Empty)
        {
            return false;
        }

        if (!principal.HasPermission(Permissions.ClinicsRead) &&
            !principal.HasPermission(Permissions.SchedulesRead) &&
            !principal.HasPermission(Permissions.SchedulesManage))
        {
            return false;
        }

        return await _clinicStore.ExistsAsync([clinicId], cancellationToken);
    }
}