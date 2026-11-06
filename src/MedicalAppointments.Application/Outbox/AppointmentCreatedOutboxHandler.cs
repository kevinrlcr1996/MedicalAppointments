using MedicalAppointments.Application.Appointments.Outbox;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Domain.Outbox;
using System.Text.Json;

namespace MedicalAppointments.Application.Outbox;

public sealed class AppointmentCreatedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public AppointmentCreatedOutboxHandler(
        IRealtimePublisher publisher,
        JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.AppointmentCreated;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<AppointmentEventPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize payload for outbox message {message.Id}.");

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
            RealtimeEvents.AppointmentCreated,
            payload,
            cancellationToken);
    }
}