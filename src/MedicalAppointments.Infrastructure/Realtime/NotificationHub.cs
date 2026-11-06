using MedicalAppointments.Application.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MedicalAppointments.Infrastructure.Realtime;

[Authorize]
public sealed class NotificationHub : Hub
{
    private readonly IRealtimeAccessService _accessService;

    public NotificationHub(IRealtimeAccessService accessService)
    {
        _accessService = accessService;
    }

    public override async Task OnConnectedAsync()
    {
        if (Context.UserIdentifier is not null &&
            Guid.TryParse(Context.UserIdentifier, out var userId))
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                RealtimeGroups.ForUser(userId));
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinDoctorGroup(Guid doctorId)
    {
        if (Context.User is null ||
            !await _accessService.CanJoinDoctorGroupAsync(Context.User, doctorId, Context.ConnectionAborted))
        {
            throw new HubException("Not authorized to join this doctor group.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            RealtimeGroups.ForDoctor(doctorId));
    }

    public async Task LeaveDoctorGroup(Guid doctorId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            RealtimeGroups.ForDoctor(doctorId));
    }

    public async Task JoinClinicGroup(Guid clinicId)
    {
        if (Context.User is null ||
            !await _accessService.CanJoinClinicGroupAsync(Context.User, clinicId, Context.ConnectionAborted))
        {
            throw new HubException("Not authorized to join this clinic group.");
        }

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            RealtimeGroups.ForClinic(clinicId));
    }

    public async Task LeaveClinicGroup(Guid clinicId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            RealtimeGroups.ForClinic(clinicId));
    }
}