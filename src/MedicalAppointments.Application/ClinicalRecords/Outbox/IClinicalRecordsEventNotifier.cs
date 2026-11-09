using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Outbox;

public interface IClinicalRecordsEventNotifier
{
    void EnqueueNoteEvent(
        ClinicalNote note,
        Appointment appointment,
        string changeType,
        DateTimeOffset now,
        string? correlationId);

    void EnqueueAttachmentEvent(
        ClinicalAttachment attachment,
        Domain.Appointments.Appointment appointment,
        string changeType,
        DateTimeOffset now,
        string? correlationId);
}