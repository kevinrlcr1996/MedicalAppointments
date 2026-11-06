using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Domain.Appointments;

public sealed class Appointment : AggregateRoot
{
    private Appointment()
    {
    }

    public Guid PatientId { get; private set; }

    public Patient Patient { get; private set; } = null!;

    public Guid DoctorId { get; private set; }

    public Doctor Doctor { get; private set; } = null!;

    public Guid ClinicId { get; private set; }

    public Clinic Clinic { get; private set; } = null!;

    public Guid? RoomId { get; private set; }

    public Room? Room { get; private set; }

    public DateOnly LocalDate { get; private set; }

    public DateTimeOffset StartAt { get; private set; }

    public DateTimeOffset EndAt { get; private set; }

    public AppointmentStatus Status { get; private set; }

    public string? Reason { get; private set; }

    public string? Notes { get; private set; }

    public Guid? OriginalAppointmentId { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public Guid? CancelledByUserId { get; private set; }

    public string? CancellationReason { get; private set; }

    public static Appointment Create(
        Guid patientId,
        Guid doctorId,
        Guid clinicId,
        Guid? roomId,
        DateOnly localDate,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        string? reason,
        string? notes,
        Guid? originalAppointmentId,
        DateTimeOffset now)
    {
        if (patientId == Guid.Empty)
        {
            throw new DomainException("Patient is required.");
        }

        if (doctorId == Guid.Empty)
        {
            throw new DomainException("Doctor is required.");
        }

        if (clinicId == Guid.Empty)
        {
            throw new DomainException("Clinic is required.");
        }

        if (endAt <= startAt)
        {
            throw new DomainException("Appointment end must be later than start.");
        }

        var durationMinutes = (endAt - startAt).TotalMinutes;

        if (durationMinutes is < 5 or > 240)
        {
            throw new DomainException("Appointment duration must be between 5 and 240 minutes.");
        }

        return new Appointment
        {
            Id = Guid.CreateVersion7(),
            PatientId = patientId,
            DoctorId = doctorId,
            ClinicId = clinicId,
            RoomId = roomId,
            LocalDate = localDate,
            StartAt = startAt,
            EndAt = endAt,
            Status = AppointmentStatus.Pending,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            OriginalAppointmentId = originalAppointmentId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Confirm(DateTimeOffset now)
    {
        TransitionTo(AppointmentStatus.Confirmed, now);
    }

    public void CheckIn(DateTimeOffset now)
    {
        TransitionTo(AppointmentStatus.CheckedIn, now);
    }

    public void Start(DateTimeOffset now)
    {
        TransitionTo(AppointmentStatus.InProgress, now);
    }

    public void Complete(DateTimeOffset now)
    {
        TransitionTo(AppointmentStatus.Completed, now);
    }

    public void MarkNoShow(DateTimeOffset now)
    {
        TransitionTo(AppointmentStatus.NoShow, now);
    }

    public void MarkRescheduled(DateTimeOffset now)
    {
        TransitionTo(AppointmentStatus.Rescheduled, now);
    }

    public void Cancel(
        DateTimeOffset now,
        Guid? cancelledByUserId,
        string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        TransitionTo(AppointmentStatus.Cancelled, now);

        CancelledAt = now;
        CancelledByUserId = cancelledByUserId;
        CancellationReason = reason.Trim();
    }

    private void TransitionTo(AppointmentStatus target, DateTimeOffset now)
    {
        AppointmentStateMachine.EnsureTransition(Status, target);
        Status = target;
        Touch(now);
    }
}