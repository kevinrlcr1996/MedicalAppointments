using System.Text.Json;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Outbox;
using MedicalAppointments.Domain.Outbox;
using MedicalAppointments.Domain.WaitingRoom;

namespace MedicalAppointments.Application.WaitingRoom.Outbox;

public sealed class WaitingRoomEventNotifier : IWaitingRoomEventNotifier
{
    private readonly IOutboxWriter _outboxWriter;
    private readonly JsonSerializerOptions _jsonOptions;

    public WaitingRoomEventNotifier(
        IOutboxWriter outboxWriter,
        JsonSerializerOptions jsonOptions)
    {
        _outboxWriter = outboxWriter;
        _jsonOptions = jsonOptions;
    }

    public void EnqueueUpdated(
        WaitingRoomCall call,
        string changeType,
        DateTimeOffset now,
        string? correlationId)
    {
        var payload = new WaitingRoomUpdatedPayload(
            call.Id, call.AppointmentId, call.DoctorId, call.ClinicId, call.RoomId,
            call.LocalDate, call.SequenceNumber, call.Status, changeType, now, correlationId);

        Enqueue(OutboxTypes.WaitingRoomUpdated, payload, now);
    }

    public void EnqueuePatientCalled(
        WaitingRoomCall call,
        Guid patientId,
        Guid? patientUserId,
        string patientFullName,
        string doctorFullName,
        string? roomName,
        DateTimeOffset now,
        string? correlationId)
    {
        var payload = new PatientCalledPayload(
            call.Id, call.AppointmentId, patientId, patientUserId, patientFullName,
            call.SequenceNumber, call.DoctorId, doctorFullName, call.ClinicId,
            call.RoomId, roomName, now, correlationId);

        Enqueue(OutboxTypes.WaitingRoomPatientCalled, payload, now);
    }

    public void EnqueuePatientRecalled(
        WaitingRoomCall call,
        Guid patientId,
        Guid? patientUserId,
        string patientFullName,
        string? roomName,
        DateTimeOffset now,
        string? correlationId)
    {
        var payload = new PatientRecalledPayload(
            call.Id, call.AppointmentId, patientId, patientUserId, patientFullName,
            call.SequenceNumber, call.RecallCount, call.DoctorId, call.ClinicId,
            call.RoomId, roomName, now, correlationId);

        Enqueue(OutboxTypes.WaitingRoomPatientRecalled, payload, now);
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