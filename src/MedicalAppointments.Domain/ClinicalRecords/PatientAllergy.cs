using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Domain.ClinicalRecords;

public sealed class PatientAllergy : AggregateRoot
{
    private PatientAllergy()
    {
    }

    public Guid PatientId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public Guid AuthorDoctorId { get; private set; }
    public string AuthorDisplayName { get; private set; } = null!;

    public string Allergen { get; private set; } = null!;
    public AllergenClass AllergenClass { get; private set; }
    public AllergySeverity Severity { get; private set; }
    public string? Reaction { get; private set; }
    public DateOnly? OnsetDate { get; private set; }

    // Ciclo de vida ORTHOGONAL a IsFinal: una alergia firmada puede resolverse (transición dedicada).
    public bool IsResolved { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }

    public bool IsFinal { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }
    public Guid? FinalizedByUserId { get; private set; }
    public Guid? CorrectsAllergyId { get; private set; }

    // Rule 6: el registro conoce al paciente; el paciente NO navega a alergias (Patient.cs intacto).
    public Patient Patient { get; private set; } = null!;
    public Doctor AuthorDoctor { get; private set; } = null!;
    public PatientAllergy? CorrectsAllergy { get; private set; }

    public static PatientAllergy Create(
        Guid patientId, Guid authorUserId, Guid authorDoctorId, string authorDisplayName,
        string allergen, AllergenClass allergenClass, AllergySeverity severity,
        string? reaction, DateOnly? onsetDate, DateTimeOffset recordedAtUtc, DateTimeOffset now)
    {
        RequirePatient(patientId);
        RequireAuthor(authorUserId, authorDoctorId, authorDisplayName);
        Validate(allergen, reaction);

        return new PatientAllergy
        {
            Id = Guid.CreateVersion7(),
            PatientId = patientId,
            AuthorUserId = authorUserId,
            AuthorDoctorId = authorDoctorId,
            AuthorDisplayName = authorDisplayName,
            Allergen = allergen.Trim(),
            AllergenClass = allergenClass,
            Severity = severity,
            Reaction = NormalizeOptional(reaction),
            OnsetDate = onsetDate,
            IsResolved = false,
            IsFinal = false,
            RecordedAtUtc = recordedAtUtc,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDraft(string allergen, AllergenClass allergenClass, AllergySeverity severity, string? reaction, DateOnly? onsetDate, DateTimeOffset now)
    {
        if (IsFinal) throw new DomainException("A finalized allergy cannot be edited.");
        Validate(allergen, reaction);
        Allergen = allergen.Trim();
        AllergenClass = allergenClass;
        Severity = severity;
        Reaction = NormalizeOptional(reaction);
        OnsetDate = onsetDate;
        Touch(now);
    }

    public void Finalize(Guid finalizedByUserId, DateTimeOffset now)
    {
        if (IsFinal) throw new DomainException("Already finalized.");
        if (finalizedByUserId == Guid.Empty) throw new DomainException("Finalizing user is required.");
        IsFinal = true;
        FinalizedAt = now;
        FinalizedByUserId = finalizedByUserId;
        Touch(now);
    }

    // Transición de ciclo de vida (NO edición de contenido): exige registro VALIDADO (IsFinal).
    public void MarkResolved(Guid resolvedByUserId, DateTimeOffset now)
    {
        if (!IsFinal) throw new DomainException("Only a finalized allergy can be resolved.");
        if (IsResolved) return; // idempotente
        if (resolvedByUserId == Guid.Empty) throw new DomainException("Resolving user is required.");
        IsResolved = true;
        ResolvedAt = now;
        ResolvedByUserId = resolvedByUserId;
        Touch(now);
    }

    public void MarkUnresolved(Guid resolvedByUserId, DateTimeOffset now)
    {
        if (!IsFinal) throw new DomainException("Only a finalized allergy can be reactivated.");
        if (!IsResolved) return;
        if (resolvedByUserId == Guid.Empty) throw new DomainException("User is required.");
        IsResolved = false;
        ResolvedAt = null;
        ResolvedByUserId = null;
        Touch(now);
    }

    public static PatientAllergy CreateCorrection(
        PatientAllergy original, Guid patientId, Guid authorUserId, Guid authorDoctorId, string authorDisplayName,
        string allergen, AllergenClass allergenClass, AllergySeverity severity,
        string? reaction, DateOnly? onsetDate, DateTimeOffset recordedAtUtc, DateTimeOffset now)
    {
        if (original is null) throw new DomainException("Original allergy is required.");
        if (!original.IsFinal) throw new DomainException("A correction can only target a finalized allergy.");
        RequirePatient(patientId);
        RequireAuthor(authorUserId, authorDoctorId, authorDisplayName);
        Validate(allergen, reaction);

        return new PatientAllergy
        {
            Id = Guid.CreateVersion7(),
            PatientId = patientId,
            AuthorUserId = authorUserId,
            AuthorDoctorId = authorDoctorId,
            AuthorDisplayName = authorDisplayName,
            Allergen = allergen.Trim(),
            AllergenClass = allergenClass,
            Severity = severity,
            Reaction = NormalizeOptional(reaction),
            OnsetDate = onsetDate,
            IsResolved = false,
            IsFinal = false,
            RecordedAtUtc = recordedAtUtc,
            CorrectsAllergyId = original.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static void RequirePatient(Guid patientId)
    {
        if (patientId == Guid.Empty) throw new DomainException("Patient is required.");
    }

    private static void RequireAuthor(Guid authorUserId, Guid authorDoctorId, string authorDisplayName)
    {
        if (authorUserId == Guid.Empty) throw new DomainException("Author user is required.");
        if (authorDoctorId == Guid.Empty) throw new DomainException("Author doctor profile is required.");
        if (string.IsNullOrWhiteSpace(authorDisplayName)) throw new DomainException("Author display name is required.");
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void Validate(string allergen, string? reaction)
    {
        if (string.IsNullOrWhiteSpace(allergen)) throw new DomainException("Allergen is required.");
        if (allergen.Trim().Length > 200) throw new DomainException("Allergen is too long.");
        if (reaction is not null && reaction.Length > 1000) throw new DomainException("Reaction is too long.");
    }
}