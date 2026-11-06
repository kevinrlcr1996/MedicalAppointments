using System.Text.Json;
using MedicalAppointments.Application.Appointments.Outbox;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Outbox;

public sealed class AppointmentCancelledOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public AppointmentCancelledOutboxHandler(
        IRealtimePublisher publisher,
        JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.AppointmentCancelled;

    public async Task HandleAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<AppointmentCancelledPayload>(
            message.Payload,
            _jsonOptions);

        if (payload is null)
        {
            throw new InvalidOperationException(
                $"Unable to deserialize payload for outbox message {message.Id}.");
        }

        var groups = new List<string>
        {
            RealtimeGroups.ForDoctor(payload.DoctorId),
            RealtimeGroups.ForClinic(payload.ClinicId),
            RealtimeGroups.ForPatient(payload.PatientId),
            RealtimeGroups.ForAppointment(payload.AppointmentId)
        };

        if (payload.PatientUserId.HasValue)
        {
            groups.Add(RealtimeGroups.ForUser(payload.PatientUserId.Value));
        }

        await _publisher.PublishToGroupsAsync(
            groups,
            RealtimeEvents.AppointmentCancelled,
            payload,
            cancellationToken);
    }
}