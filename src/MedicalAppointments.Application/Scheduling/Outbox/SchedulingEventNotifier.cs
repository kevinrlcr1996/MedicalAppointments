using System.Text.Json;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Outbox;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Scheduling.Outbox;

public sealed class SchedulingEventNotifier : ISchedulingEventNotifier
{
    private readonly IOutboxWriter _outboxWriter;
    private readonly TimeProvider _timeProvider;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public SchedulingEventNotifier(
        IOutboxWriter outboxWriter,
        TimeProvider timeProvider,
        ICorrelationIdProvider correlationIdProvider,
        JsonSerializerOptions jsonOptions)
    {
        _outboxWriter = outboxWriter;
        _timeProvider = timeProvider;
        _correlationIdProvider = correlationIdProvider;
        _jsonOptions = jsonOptions;
    }

    public void EnqueueScheduleChanged(
        Guid scheduleId,
        Guid doctorId,
        Guid clinicId,
        string changeType,
        DateOnly affectedFrom,
        DateOnly affectedTo)
    {
        var now = _timeProvider.GetUtcNow();

        var payload = new ScheduleChangedPayload(
            scheduleId,
            doctorId,
            clinicId,
            changeType,
            affectedFrom,
            affectedTo,
            now,
            _correlationIdProvider.CorrelationId);

        Enqueue(OutboxTypes.ScheduleChanged, payload, now);
    }

    public void EnqueueAvailabilityChanged(
        Guid doctorId,
        Guid clinicId,
        DateOnly affectedFrom,
        DateOnly affectedTo)
    {
        var now = _timeProvider.GetUtcNow();

        var payload = new AvailabilityChangedPayload(
            doctorId,
            clinicId,
            affectedFrom,
            affectedTo,
            now,
            _correlationIdProvider.CorrelationId);

        Enqueue(OutboxTypes.AvailabilityChanged, payload, now);
    }

    private void Enqueue(string type, object payload, DateTimeOffset now)
    {
        var serialized = JsonSerializer.Serialize(payload, _jsonOptions);

        var message = OutboxMessage.Create(
            type: type,
            payload: serialized,
            occurredAt: now,
            createdAt: now);

        _outboxWriter.Enqueue(message);
    }
}