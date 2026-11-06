using MedicalAppointments.Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace MedicalAppointments.Infrastructure.Realtime;

public sealed class SignalRRealtimePublisher : IRealtimePublisher
{
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly IHubContext<FoundationHub> _foundationHub;

    public SignalRRealtimePublisher(
        IHubContext<NotificationHub> notificationHub,
        IHubContext<FoundationHub> foundationHub)
    {
        _notificationHub = notificationHub;
        _foundationHub = foundationHub;
    }

    public Task PublishToGroupAsync(
        string groupName,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        return _foundationHub.Clients.Group(groupName).SendAsync(eventName, payload, cancellationToken);
    }

    public Task PublishToUserAsync(
        Guid userId,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        var groupName = RealtimeGroups.ForUser(userId);

        return _notificationHub.Clients.Group(groupName).SendAsync(eventName, payload, cancellationToken);
    }

    public Task PublishToGroupsAsync(
        IEnumerable<string> groupNames,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        var groups = groupNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (groups.Length == 0)
        {
            return Task.CompletedTask;
        }

        return _notificationHub.Clients.Groups(groups).SendAsync(eventName, payload, cancellationToken);
    }
}