using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Domain.ClinicalRecords;

public enum PrescriptionStatus { Draft = 0, Issued = 1, Dispensed = 2, Expired = 3, Cancelled = 4 }

public sealed class Prescription : AggregateRoot
{
    private Prescription() { }

    public Guid PatientId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public Guid AuthorDoctorId { get; private set; }
    public string AuthorDisplayName { get; private set; } = null!;

    public string MedicationName { get; private set; } = null!;
    public string? Strength { get; private set; }
    public string? DosageForm { get; private set; }
    public string? Frequency { get; private set; }
    public string? Route { get; private set; }
    public int? Quantity { get; private set; }
    public int? RefillsAllowed { get; private set; }
    public int? DaysSupply { get; private set; }
    public string? Instructions { get; private set; }
    public string? Note { get; private set; }

    public PrescriptionStatus Status { get; private set; }
    public bool IsFinal { get; private set; }

    public DateTimeOffset RecordedAtUtc { get; private set; }
    public DateTimeOffset? IssuedAt { get; private set; }
    public Guid? IssuedByUserId { get; private set; }
    public DateTimeOffset? DispensedAt { get; private set; }
    public Guid? DispensedByUserId { get; private set; }
    public DateTimeOffset? ExpiredAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public Guid? CancelledByUserId { get; private set; }

    public Guid? CorrectsPrescriptionId { get; private set; }
    public Guid? RenewedFromPrescriptionId { get; private set; }

    public Patient Patient { get; private set; } = null!;
    public Doctor AuthorDoctor { get; private set; } = null!;
    public Prescription? CorrectsPrescription { get; private set; }
    public Prescription? RenewedFromPrescription { get; private set; }

    public static Prescription Create(
        Guid patientId, Guid authorUserId, Guid authorDoctorId, string authorDisplayName,
        string medicationName, string? strength, string? dosageForm, string? frequency, string? route,
        int? quantity, int? refillsAllowed, int? daysSupply, string? instructions, string? note,
        DateTimeOffset recordedAtUtc, DateTimeOffset now)
    {
        RequirePatient(patientId); RequireAuthor(authorUserId, authorDoctorId, authorDisplayName);
        ValidateContent(medicationName, strength, dosageForm, frequency, route, quantity, refillsAllowed, daysSupply, instructions, note);
        return new Prescription
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
            Quantity = quantity,
            RefillsAllowed = refillsAllowed,
            DaysSupply = daysSupply,
            Instructions = NormalizeOptional(instructions),
            Note = NormalizeOptional(note),
            Status = PrescriptionStatus.Draft,
            IsFinal = false,
            RecordedAtUtc = recordedAtUtc,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDraft(string medicationName, string? strength, string? dosageForm, string? frequency,
        string? route, int? quantity, int? refillsAllowed, int? daysSupply, string? instructions, string? note, DateTimeOffset now)
    {
        if (IsFinal) throw new DomainException("A finalized (issued) prescription cannot be edited; issue a correction instead.");
        ValidateContent(medicationName, strength, dosageForm, frequency, route, quantity, refillsAllowed, daysSupply, instructions, note);
        MedicationName = medicationName.Trim(); Strength = NormalizeOptional(strength); DosageForm = NormalizeOptional(dosageForm);
        Frequency = NormalizeOptional(frequency); Route = NormalizeOptional(route); Quantity = quantity;
        RefillsAllowed = refillsAllowed; DaysSupply = daysSupply; Instructions = NormalizeOptional(instructions); Note = NormalizeOptional(note);
        Touch(now);
    }

    public void Issue(Guid issuedByUserId, DateTimeOffset now)
    {
        if (IsFinal) throw new DomainException("Already issued.");
        if (issuedByUserId == Guid.Empty) throw new DomainException("Issuing user is required.");
        Status = PrescriptionStatus.Issued; IsFinal = true; IssuedAt = now; IssuedByUserId = issuedByUserId; Touch(now);
    }

    public void Dispense(Guid byUserId, DateTimeOffset now) // 9.1.2
    {
        if (!IsFinal) throw new DomainException("Only an issued prescription can be dispensed.");
        if (Status == PrescriptionStatus.Dispensed) return;
        if (Status != PrescriptionStatus.Issued) throw new DomainException("Only an issued prescription can be dispensed.");
        if (byUserId == Guid.Empty) throw new DomainException("User is required.");
        Status = PrescriptionStatus.Dispensed; DispensedAt = now; DispensedByUserId = byUserId; Touch(now);
    }

    public void Expire(DateTimeOffset now) // 9.1.2
    {
        if (Status == PrescriptionStatus.Expired) return;
        if (Status != PrescriptionStatus.Issued && Status != PrescriptionStatus.Dispensed)
            throw new DomainException("Only an issued or dispensed prescription can expire.");
        Status = PrescriptionStatus.Expired; ExpiredAt = now; Touch(now);
    }

    public void Cancel(Guid byUserId, DateTimeOffset now)
    {
        if (Status == PrescriptionStatus.Cancelled) return;
        if (Status == PrescriptionStatus.Expired) throw new DomainException("An expired prescription cannot be cancelled.");
        if (Status != PrescriptionStatus.Draft && Status != PrescriptionStatus.Issued && Status != PrescriptionStatus.Dispensed)
            throw new DomainException("Invalid state to cancel.");
        if (byUserId == Guid.Empty) throw new DomainException("User is required.");
        Status = PrescriptionStatus.Cancelled; CancelledAt = now; CancelledByUserId = byUserId; Touch(now);
    }

    public static Prescription Renew(Prescription original, Guid authorUserId, Guid authorDoctorId, string authorDisplayName, DateTimeOffset now)
    {
        if (original is null) throw new DomainException("Original prescription is required.");
        if (original.Status != PrescriptionStatus.Issued && original.Status != PrescriptionStatus.Dispensed)
            throw new DomainException("Only an issued or dispensed prescription can be renewed.");
        RequireAuthor(authorUserId, authorDoctorId, authorDisplayName);
        return new Prescription
        {
            Id = Guid.CreateVersion7(),
            PatientId = original.PatientId,
            AuthorUserId = authorUserId,
            AuthorDoctorId = authorDoctorId,
            AuthorDisplayName = authorDisplayName.Trim(),
            MedicationName = original.MedicationName,
            Strength = original.Strength,
            DosageForm = original.DosageForm,
            Frequency = original.Frequency,
            Route = original.Route,
            Quantity = original.Quantity,
            RefillsAllowed = original.RefillsAllowed,
            DaysSupply = original.DaysSupply,
            Instructions = original.Instructions,
            Note = original.Note,
            Status = PrescriptionStatus.Issued,
            IsFinal = true,
            RecordedAtUtc = now,
            IssuedAt = now,
            IssuedByUserId = authorUserId,
            RenewedFromPrescriptionId = original.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static Prescription CreateCorrection(Prescription original, Guid patientId, Guid authorUserId, Guid authorDoctorId,
        string authorDisplayName, string medicationName, string? strength, string? dosageForm, string? frequency, string? route,
        int? quantity, int? refillsAllowed, int? daysSupply, string? instructions, string? note, DateTimeOffset recordedAtUtc, DateTimeOffset now)
    {
        if (original is null) throw new DomainException("Original prescription is required.");
        if (!original.IsFinal) throw new DomainException("A correction can only target a finalized (issued) prescription.");
        RequirePatient(patientId); RequireAuthor(authorUserId, authorDoctorId, authorDisplayName);
        ValidateContent(medicationName, strength, dosageForm, frequency, route, quantity, refillsAllowed, daysSupply, instructions, note);
        return new Prescription
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
            Quantity = quantity,
            RefillsAllowed = refillsAllowed,
            DaysSupply = daysSupply,
            Instructions = NormalizeOptional(instructions),
            Note = NormalizeOptional(note),
            Status = PrescriptionStatus.Draft,
            IsFinal = false,
            RecordedAtUtc = recordedAtUtc,
            CorrectsPrescriptionId = original.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static void RequirePatient(Guid patientId) { if (patientId == Guid.Empty) throw new DomainException("Patient is required."); }
    private static void RequireAuthor(Guid u, Guid d, string n) { if (u == Guid.Empty) throw new DomainException("Author user is required."); if (d == Guid.Empty) throw new DomainException("Author doctor profile is required."); if (string.IsNullOrWhiteSpace(n)) throw new DomainException("Author display name is required."); }
    private static string? NormalizeOptional(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    private static void ValidateContent(string medicationName, string? strength, string? dosageForm, string? frequency, string? route,
        int? quantity, int? refillsAllowed, int? daysSupply, string? instructions, string? note)
    {
        if (string.IsNullOrWhiteSpace(medicationName)) throw new DomainException("Medication name is required.");
        if (medicationName.Trim().Length > 200) throw new DomainException("Medication name is too long.");
        if (strength is not null && strength.Length > 100) throw new DomainException("Strength is too long.");
        if (dosageForm is not null && dosageForm.Length > 100) throw new DomainException("Dosage form is too long.");
        if (frequency is not null && frequency.Length > 100) throw new DomainException("Frequency is too long.");
        if (route is not null && route.Length > 100) throw new DomainException("Route is too long.");
        if (quantity < 0) throw new DomainException("Quantity cannot be negative.");
        if (refillsAllowed < 0) throw new DomainException("Refills allowed cannot be negative.");
        if (daysSupply < 0) throw new DomainException("Days supply cannot be negative.");
        if (instructions is not null && instructions.Length > 2000) throw new DomainException("Instructions are too long.");
        if (note is not null && note.Length > 2000) throw new DomainException("Note is too long.");
    }
}