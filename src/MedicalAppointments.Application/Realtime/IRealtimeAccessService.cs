using MedicalAppointments.Application.Authorization;
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

    //Task<bool> CanJoinRoomGroupAsync(
    //    ClaimsPrincipal principal,
    //    Guid roomId,
    //    CancellationToken cancellationToken = default);

    public Task<bool> CanJoinRoomGroupAsync(
        ClaimsPrincipal principal,
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        if (roomId == Guid.Empty)
        {
            return Task.FromResult(false);
        }

        var allowed = principal.HasPermission(Permissions.WaitingRoomRead) ||
                      principal.HasPermission(Permissions.WaitingRoomManage) ||
                      principal.HasPermission(Permissions.RoomsRead);

        return Task.FromResult(allowed);
    }
}