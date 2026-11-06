using MedicalAppointments.Domain.Appointments;

namespace MedicalAppointments.Application.Appointments.Outbox;

public interface IAppointmentEventNotifier
{
    void EnqueueCreated(
        Appointment appointment,
        Guid? patientUserId,
        DateTimeOffset now,
        string? correlationId);

    void EnqueueStatusChanged(
        Appointment appointment,
        Guid? patientUserId,
        AppointmentStatus oldStatus,
        AppointmentStatus newStatus,
        DateTimeOffset now,
        string? correlationId);

    void EnqueueCancelled(
        Appointment appointment,
        Guid? patientUserId,
        AppointmentStatus oldStatus,
        DateTimeOffset now,
        string? correlationId);

    void EnqueueRescheduled(
        Appointment oldAppointment,
        Appointment newAppointment,
        Guid? patientUserId,
        AppointmentStatus oldStatus,
        DateTimeOffset now,
        string? correlationId);
}