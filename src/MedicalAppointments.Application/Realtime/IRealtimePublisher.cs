namespace MedicalAppointments.Application.Realtime;

public interface IRealtimePublisher
{
    Task PublishToGroupAsync(
        string groupName,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default);

    Task PublishToUserAsync(
        Guid userId,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default);

    Task PublishToGroupsAsync(
        IEnumerable<string> groupNames,
        string eventName,
        object payload,
        CancellationToken cancellationToken = default);
}