using System.Text.Json;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Outbox;
using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Appointments.Outbox;

public sealed class AppointmentEventNotifier : IAppointmentEventNotifier
{
    private readonly IOutboxWriter _outboxWriter;
    private readonly JsonSerializerOptions _jsonOptions;

    public AppointmentEventNotifier(
        IOutboxWriter outboxWriter,
        JsonSerializerOptions jsonOptions)
    {
        _outboxWriter = outboxWriter;
        _jsonOptions = jsonOptions;
    }

    public void EnqueueCreated(
        Appointment appointment,
        Guid? patientUserId,
        DateTimeOffset now,
        string? correlationId)
    {
        var payload = new AppointmentEventPayload(
            appointment.Id,
            appointment.PatientId,
            patientUserId,
            appointment.DoctorId,
            appointment.ClinicId,
            appointment.RoomId,
            appointment.StartAt,
            appointment.EndAt,
            appointment.Status,
            now,
            correlationId);

        Enqueue(OutboxTypes.AppointmentCreated, payload, now);
    }

    public void EnqueueStatusChanged(
        Appointment appointment,
        Guid? patientUserId,
        AppointmentStatus oldStatus,
        AppointmentStatus newStatus,
        DateTimeOffset now,
        string? correlationId)
    {
        var payload = new AppointmentStatusChangedPayload(
            appointment.Id,
            appointment.PatientId,
            patientUserId,
            appointment.DoctorId,
            appointment.ClinicId,
            appointment.RoomId,
            oldStatus,
            newStatus,
            now,
            correlationId);

        Enqueue(OutboxTypes.AppointmentStatusChanged, payload, now);
    }

    public void EnqueueCancelled(
        Appointment appointment,
        Guid? patientUserId,
        AppointmentStatus oldStatus,
        DateTimeOffset now,
        string? correlationId)
    {
        var payload = new AppointmentCancelledPayload(
            appointment.Id,
            appointment.PatientId,
            patientUserId,
            appointment.DoctorId,
            appointment.ClinicId,
            appointment.RoomId,
            oldStatus,
            appointment.CancelledAt ?? now,
            appointment.CancelledByUserId,
            now,
            correlationId);

        Enqueue(OutboxTypes.AppointmentCancelled, payload, now);
    }

    public void EnqueueRescheduled(
        Appointment oldAppointment,
        Appointment newAppointment,
        Guid? patientUserId,
        AppointmentStatus oldStatus,
        DateTimeOffset now,
        string? correlationId)
    {
        var payload = new AppointmentRescheduledPayload(
            oldAppointment.Id,
            newAppointment.Id,
            oldAppointment.PatientId,
            patientUserId,
            oldAppointment.DoctorId,
            oldAppointment.ClinicId,
            oldAppointment.RoomId,
            oldStatus,
            oldAppointment.StartAt,
            newAppointment.StartAt,
            newAppointment.EndAt,
            now,
            correlationId);

        Enqueue(OutboxTypes.AppointmentRescheduled, payload, now);
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