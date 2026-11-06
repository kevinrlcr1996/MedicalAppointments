using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MedicalAppointments.Infrastructure.Realtime;

[Authorize(Policy = Policies.RealtimeTest)]
public sealed class FoundationHub : Hub
{
    public async Task JoinTestGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, RealtimeGroups.TestFoundation);
    }

    public async Task LeaveTestGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, RealtimeGroups.TestFoundation);
    }
}