using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Domain.WaitingList;

public sealed class WaitingListEntry : AggregateRoot
{
    private WaitingListEntry()
    {
    }

    public Guid PatientId { get; private set; }

    public Guid DoctorId { get; private set; }

    public Guid? ClinicId { get; private set; }

    public DateTimeOffset PreferredFromUtc { get; private set; }

    public DateTimeOffset PreferredToUtc { get; private set; }

    public WaitingListPriority Priority { get; private set; }

    public WaitingListEntryStatus Status { get; private set; }

    public string? Note { get; private set; }

    public Patient Patient { get; private set; } = null!;

    public Doctor Doctor { get; private set; } = null!;

    public Clinic? Clinic { get; private set; }

    public static WaitingListEntry Create(
        Guid patientId,
        Guid doctorId,
        Guid? clinicId,
        DateTimeOffset preferredFromUtc,
        DateTimeOffset preferredToUtc,
        WaitingListPriority priority,
        string? note,
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

        ValidateWindow(preferredFromUtc, preferredToUtc);

        return new WaitingListEntry
        {
            Id = Guid.CreateVersion7(),
            PatientId = patientId,
            DoctorId = doctorId,
            ClinicId = clinicId,
            PreferredFromUtc = preferredFromUtc,
            PreferredToUtc = preferredToUtc,
            Priority = priority,
            Status = WaitingListEntryStatus.Waiting,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateWindow(
        Guid? clinicId,
        DateTimeOffset preferredFromUtc,
        DateTimeOffset preferredToUtc,
        WaitingListPriority priority,
        string? note,
        DateTimeOffset now)
    {
        if (Status != WaitingListEntryStatus.Waiting)
        {
            throw new DomainException("Only waiting entries can be edited.");
        }

        ValidateWindow(preferredFromUtc, preferredToUtc);

        ClinicId = clinicId;
        PreferredFromUtc = preferredFromUtc;
        PreferredToUtc = preferredToUtc;
        Priority = priority;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        Touch(now);
    }

    public void MarkFulfilled(DateTimeOffset now)
    {
        if (Status != WaitingListEntryStatus.Waiting)
        {
            throw new DomainException($"Cannot fulfil entry from status {Status}.");
        }

        Status = WaitingListEntryStatus.Fulfilled;
        Touch(now);
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status is WaitingListEntryStatus.Fulfilled or WaitingListEntryStatus.Cancelled)
        {
            throw new DomainException($"Cannot cancel entry from status {Status}.");
        }

        Status = WaitingListEntryStatus.Cancelled;
        Touch(now);
    }

    public bool MatchesSlot(DateTimeOffset slotStartUtc, DateTimeOffset slotEndUtc, Guid slotDoctorId, Guid? slotClinicId)
    {
        if (Status != WaitingListEntryStatus.Waiting)
        {
            return false;
        }

        if (DoctorId != slotDoctorId)
        {
            return false;
        }

        // ClinicId null en la entrada = acepta cualquier clínica del médico.
        if (ClinicId.HasValue && slotClinicId.HasValue && ClinicId.Value != slotClinicId.Value)
        {
            return false;
        }

        // El slot debe caer dentro de la ventana preferida (inclusive en bordes).
        return slotStartUtc >= PreferredFromUtc && slotEndUtc <= PreferredToUtc;
    }

    private static void ValidateWindow(DateTimeOffset from, DateTimeOffset to)
    {
        if (to <= from)
        {
            throw new DomainException("Preferred window end must be later than start.");
        }
    }
}