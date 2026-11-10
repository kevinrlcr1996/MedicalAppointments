using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Domain.ClinicalRecords;

public sealed class PatientMedication : AggregateRoot
{
    private PatientMedication() { }

    public Guid PatientId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public Guid AuthorDoctorId { get; private set; }
    public string AuthorDisplayName { get; private set; } = null!;

    public string MedicationName { get; private set; } = null!;
    public string? Strength { get; private set; }
    public string? DosageForm { get; private set; }
    public string? Frequency { get; private set; }
    public string? Route { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public string? Indication { get; private set; }

    public MedicationStatus Status { get; private set; }
    public DateTimeOffset? SuspendedAt { get; private set; }
    public DateTimeOffset? DiscontinuedAt { get; private set; }
    public Guid? StatusChangedByUserId { get; private set; }

    public bool IsFinal { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }
    public Guid? FinalizedByUserId { get; private set; }
    public Guid? CorrectsMedicationId { get; private set; }

    public Patient Patient { get; private set; } = null!;
    public Doctor AuthorDoctor { get; private set; } = null!;
    public PatientMedication? CorrectsMedication { get; private set; }

    public static PatientMedication Create(
        Guid patientId, Guid authorUserId, Guid authorDoctorId, string authorDisplayName,
        string medicationName, string? strength, string? dosageForm, string? frequency, string? route,
        DateOnly? startDate, string? indication, DateTimeOffset recordedAtUtc, DateTimeOffset now)
    {
        RequirePatient(patientId);
        RequireAuthor(authorUserId, authorDoctorId, authorDisplayName);
        Validate(medicationName, indication);

        return new PatientMedication
        {
            Id = Guid.CreateVersion7(),
            PatientId = patientId,
            AuthorUserId = authorUserId,
            AuthorDoctorId = authorDoctorId,
            AuthorDisplayName = authorDisplayName,
            MedicationName = medicationName.Trim(),
            Strength = NormalizeOptional(strength),
            DosageForm = NormalizeOptional(dosageForm),
            Frequency = NormalizeOptional(frequency),
            Route = NormalizeOptional(route),
            StartDate = startDate,
            Indication = NormalizeOptional(indication),
            Status = MedicationStatus.Active,
            IsFinal = false,
            RecordedAtUtc = recordedAtUtc,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDraft(string medicationName, string? strength, string? dosageForm, string? frequency, string? route, DateOnly? startDate, string? indication, DateTimeOffset now)
    {
        if (IsFinal) throw new DomainException("A finalized medication cannot be edited.");
        Validate(medicationName, indication);
        MedicationName = medicationName.Trim(); Strength = NormalizeOptional(strength); DosageForm = NormalizeOptional(dosageForm);
        Frequency = NormalizeOptional(frequency); Route = NormalizeOptional(route); StartDate = startDate; Indication = NormalizeOptional(indication);
        Touch(now);
    }

    public void Finalize(Guid finalizedByUserId, DateTimeOffset now)
    {
        if (IsFinal) throw new DomainException("Already finalized.");
        if (finalizedByUserId == Guid.Empty) throw new DomainException("Finalizing user is required.");
        IsFinal = true; FinalizedAt = now; FinalizedByUserId = finalizedByUserId; Touch(now);
    }

    public void Suspend(Guid byUserId, DateTimeOffset now)
    {
        if (!IsFinal) throw new DomainException("Only a finalized medication can be suspended.");
        if (Status == MedicationStatus.Discontinued) throw new DomainException("A discontinued medication cannot be suspended.");
        if (Status == MedicationStatus.Suspended) return;
        if (byUserId == Guid.Empty) throw new DomainException("User is required.");
        Status = MedicationStatus.Suspended; SuspendedAt = now; StatusChangedByUserId = byUserId; Touch(now);
    }

    public void Resume(Guid byUserId, DateTimeOffset now)
    {
        if (!IsFinal) throw new DomainException("Only a finalized medication can be resumed.");
        if (Status != MedicationStatus.Suspended) throw new DomainException("Only a suspended medication can be resumed.");
        if (byUserId == Guid.Empty) throw new DomainException("User is required.");
        Status = MedicationStatus.Active; SuspendedAt = null; StatusChangedByUserId = byUserId; Touch(now);
    }

    public void Discontinue(Guid byUserId, DateTimeOffset now)
    {
        if (!IsFinal) throw new DomainException("Only a finalized medication can be discontinued.");
        if (Status == MedicationStatus.Discontinued) return;
        if (byUserId == Guid.Empty) throw new DomainException("User is required.");
        Status = MedicationStatus.Discontinued; DiscontinuedAt = now; StatusChangedByUserId = byUserId; Touch(now);
    }

    public static PatientMedication CreateCorrection(
        PatientMedication original, Guid patientId, Guid authorUserId, Guid authorDoctorId, string authorDisplayName,
        string medicationName, string? strength, string? dosageForm, string? frequency, string? route,
        DateOnly? startDate, string? indication, DateTimeOffset recordedAtUtc, DateTimeOffset now)
    {
        if (original is null) throw new DomainException("Original medication is required.");
        if (!original.IsFinal) throw new DomainException("A correction can only target a finalized medication.");
        RequirePatient(patientId); RequireAuthor(authorUserId, authorDoctorId, authorDisplayName); Validate(medicationName, indication);
        return new PatientMedication
        {
            Id = Guid.CreateVersion7(),
            PatientId = patientId,
            AuthorUserId = authorUserId,
            AuthorDoctorId = authorDoctorId,
            AuthorDisplayName = authorDisplayName,
            MedicationName = medicationName.Trim(),
            Strength = NormalizeOptional(strength),
            DosageForm = NormalizeOptional(dosageForm),
            Frequency = NormalizeOptional(frequency),
            Route = NormalizeOptional(route),
            StartDate = startDate,
            Indication = NormalizeOptional(indication),
            Status = MedicationStatus.Active,
            IsFinal = false,
            RecordedAtUtc = recordedAtUtc,
            CorrectsMedicationId = original.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static void RequirePatient(Guid patientId) { if (patientId == Guid.Empty) throw new DomainException("Patient is required."); }
    private static void RequireAuthor(Guid u, Guid d, string n) { if (u == Guid.Empty) throw new DomainException("Author user is required."); if (d == Guid.Empty) throw new DomainException("Author doctor profile is required."); if (string.IsNullOrWhiteSpace(n)) throw new DomainException("Author display name is required."); }
    private static string? NormalizeOptional(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static void Validate(string name, string? indication)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Medication name is required.");
        if (name.Trim().Length > 200) throw new DomainException("Medication name is too long.");
        if (indication is not null && indication.Length > 1000) throw new DomainException("Indication is too long.");
    }
}