using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Domain.WaitingRoom;

public sealed class WaitingRoomCall : AggregateRoot
{
    private WaitingRoomCall()
    {
    }

    public Guid AppointmentId { get; private set; }

    public Guid DoctorId { get; private set; }

    public Guid ClinicId { get; private set; }

    public Guid? RoomId { get; private set; }

    public DateOnly LocalDate { get; private set; }

    public int SequenceNumber { get; private set; }

    public WaitingRoomCallStatus Status { get; private set; }

    public int RecallCount { get; private set; }

    public DateTimeOffset? CalledAt { get; private set; }

    public DateTimeOffset? AnsweredAt { get; private set; }

    public DateTimeOffset? SkippedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string? Note { get; private set; }

    public Appointment Appointment { get; private set; } = null!;

    public Doctor Doctor { get; private set; } = null!;

    public Clinic Clinic { get; private set; } = null!;

    public Room? Room { get; private set; }

    public static WaitingRoomCall Queue(
        Guid appointmentId,
        Guid doctorId,
        Guid clinicId,
        DateOnly localDate,
        int sequenceNumber,
        string? note,
        DateTimeOffset now)
    {
        if (appointmentId == Guid.Empty)
        {
            throw new DomainException("Appointment is required.");
        }

        if (doctorId == Guid.Empty)
        {
            throw new DomainException("Doctor is required.");
        }

        if (clinicId == Guid.Empty)
        {
            throw new DomainException("Clinic is required.");
        }

        if (sequenceNumber < 1)
        {
            throw new DomainException("Sequence number must be positive.");
        }

        return new WaitingRoomCall
        {
            Id = Guid.CreateVersion7(),
            AppointmentId = appointmentId,
            DoctorId = doctorId,
            ClinicId = clinicId,
            LocalDate = localDate,
            SequenceNumber = sequenceNumber,
            Status = WaitingRoomCallStatus.Queued,
            RecallCount = 0,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkCalled(Guid? roomId, DateTimeOffset now)
    {
        if (Status is not (WaitingRoomCallStatus.Queued or WaitingRoomCallStatus.Skipped))
        {
            throw new DomainException($"Cannot call a {Status} patient.");
        }

        RoomId = roomId;
        Status = WaitingRoomCallStatus.Called;
        CalledAt = now;
        Touch(now);
    }

    // FIX: "recordar" unifica re-anunciar a un Llamado y re-llamar a un Saltado.
    // Ambos vuelven/dejan la call en Called con RecallCount++. Antes exigía Called
    // y rompía el caso Skipped que la UI (columna "Saltados" -> botón Recordar) expone.
    public void MarkRecalled(DateTimeOffset now)
    {
        if (Status is not (WaitingRoomCallStatus.Called or WaitingRoomCallStatus.Skipped))
        {
            throw new DomainException("Only a called or skipped patient can be recalled.");
        }

        RecallCount += 1;
        Status = WaitingRoomCallStatus.Called;
        CalledAt = now;
        Touch(now);
    }

    public void MarkInConsultation(DateTimeOffset now)
    {
        if (Status != WaitingRoomCallStatus.Called)
        {
            throw new DomainException("Only a called patient can enter consultation.");
        }

        Status = WaitingRoomCallStatus.InConsultation;
        AnsweredAt = now;
        Touch(now);
    }

    public void MarkSkipped(DateTimeOffset now)
    {
        if (Status != WaitingRoomCallStatus.Called)
        {
            throw new DomainException("Only a called patient can be skipped.");
        }

        Status = WaitingRoomCallStatus.Skipped;
        SkippedAt = now;
        Touch(now);
    }

    public void MarkCompleted(DateTimeOffset now)
    {
        if (Status is not (WaitingRoomCallStatus.InConsultation or WaitingRoomCallStatus.Called))
        {
            throw new DomainException($"Cannot complete a {Status} call.");
        }

        Status = WaitingRoomCallStatus.Completed;
        CompletedAt = now;
        Touch(now);
    }

    public bool IsInQueue()
    {
        return Status is WaitingRoomCallStatus.Queued or WaitingRoomCallStatus.Skipped;
    }
}