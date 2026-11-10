using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.MedicalStructure;
using System.Text.RegularExpressions;

namespace MedicalAppointments.Domain.ClinicalRecords;

public sealed class Diagnosis : AggregateRoot
{
    // ICD-10 sintáctico: letra A-T o V-Z (U reservado), 2 dígitos, punto opcional + 1-4 alfanuméricos.
    // Validación SINTÁCTICA solo; NO se valida semántica contra catálogo (deuda 8d.2b / hardening futuro).
    private static readonly Regex Icd10Pattern = new(
        "^[A-TV-Z][0-9][0-9](\\.[0-9A-Z]{1,4})?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private Diagnosis()
    {
    }

    public Guid AppointmentId { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public Guid AuthorDoctorId { get; private set; }
    public string AuthorDisplayName { get; private set; } = null!;

    public string Code { get; private set; } = null!;
    public string DisplayTerm { get; private set; } = null!;
    public DiagnosisCategory Category { get; private set; }
    public DateOnly? OnsetDate { get; private set; }
    public string? Note { get; private set; }

    public bool IsFinal { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }
    public Guid? FinalizedByUserId { get; private set; }
    public Guid? CorrectsDiagnosisId { get; private set; }

    // Rule 6: el diagnóstico conoce la cita; la cita NO navega a diagnósticos (Appointment intacto).
    public Appointment Appointment { get; private set; } = null!;
    public Doctor AuthorDoctor { get; private set; } = null!;
    public Diagnosis? CorrectsDiagnosis { get; private set; }

    public static Diagnosis Create(
        Guid appointmentId,
        Guid authorUserId,
        Guid authorDoctorId,
        string authorDisplayName,
        string code,
        string displayTerm,
        DiagnosisCategory category,
        DateOnly? onsetDate,
        string? note,
        DateTimeOffset recordedAtUtc,
        DateTimeOffset now)
    {
        RequireAppointment(appointmentId);
        RequireAuthor(authorUserId, authorDoctorId, authorDisplayName);
        Validate(code, displayTerm, note);

        return new Diagnosis
        {
            Id = Guid.CreateVersion7(),
            AppointmentId = appointmentId,
            AuthorUserId = authorUserId,
            AuthorDoctorId = authorDoctorId,
            AuthorDisplayName = authorDisplayName,
            Code = NormalizeCode(code),
            DisplayTerm = displayTerm.Trim(),
            Category = category,
            OnsetDate = onsetDate,
            Note = NormalizeNote(note),
            IsFinal = false,
            RecordedAtUtc = recordedAtUtc,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDraft(string code, string displayTerm, DiagnosisCategory category, DateOnly? onsetDate, string? note, DateTimeOffset now)
    {
        if (IsFinal)
        {
            throw new DomainException("A finalized diagnosis cannot be edited.");
        }

        Validate(code, displayTerm, note);

        Code = NormalizeCode(code);
        DisplayTerm = displayTerm.Trim();
        Category = category;
        OnsetDate = onsetDate;
        Note = NormalizeNote(note);
        Touch(now);
    }

    public void Finalize(Guid finalizedByUserId, DateTimeOffset now)
    {
        if (IsFinal)
        {
            throw new DomainException("Already finalized.");
        }

        if (finalizedByUserId == Guid.Empty)
        {
            throw new DomainException("Finalizing user is required.");
        }

        IsFinal = true;
        FinalizedAt = now;
        FinalizedByUserId = finalizedByUserId;
        Touch(now);
    }

    public static Diagnosis CreateCorrection(
        Diagnosis original,
        Guid appointmentId,
        Guid authorUserId,
        Guid authorDoctorId,
        string authorDisplayName,
        string code,
        string displayTerm,
        DiagnosisCategory category,
        DateOnly? onsetDate,
        string? note,
        DateTimeOffset recordedAtUtc,
        DateTimeOffset now)
    {
        if (original is null)
        {
            throw new DomainException("Original diagnosis is required.");
        }

        if (!original.IsFinal)
        {
            throw new DomainException("A correction can only target a finalized diagnosis.");
        }

        RequireAppointment(appointmentId);
        RequireAuthor(authorUserId, authorDoctorId, authorDisplayName);
        Validate(code, displayTerm, note);

        return new Diagnosis
        {
            Id = Guid.CreateVersion7(),
            AppointmentId = appointmentId,
            AuthorUserId = authorUserId,
            AuthorDoctorId = authorDoctorId,
            AuthorDisplayName = authorDisplayName,
            Code = NormalizeCode(code),
            DisplayTerm = displayTerm.Trim(),
            Category = category,
            OnsetDate = onsetDate,
            Note = NormalizeNote(note),
            IsFinal = false,
            RecordedAtUtc = recordedAtUtc,
            CorrectsDiagnosisId = original.Id,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static void RequireAppointment(Guid appointmentId)
    {
        if (appointmentId == Guid.Empty)
        {
            throw new DomainException("Appointment is required.");
        }
    }

    private static void RequireAuthor(Guid authorUserId, Guid authorDoctorId, string authorDisplayName)
    {
        if (authorUserId == Guid.Empty)
        {
            throw new DomainException("Author user is required.");
        }

        if (authorDoctorId == Guid.Empty)
        {
            throw new DomainException("Author doctor profile is required.");
        }

        if (string.IsNullOrWhiteSpace(authorDisplayName))
        {
            throw new DomainException("Author display name is required.");
        }
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    private static string? NormalizeNote(string? note) =>
        string.IsNullOrWhiteSpace(note) ? null : note.Trim();

    private static void Validate(string code, string displayTerm, string? note)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Diagnosis code is required.");
        }

        if (!Icd10Pattern.IsMatch(NormalizeCode(code)))
        {
            throw new DomainException("Diagnosis code is not a valid ICD-10 code (expected e.g. I10, E11.9).");
        }

        if (string.IsNullOrWhiteSpace(displayTerm))
        {
            throw new DomainException("Diagnosis display term is required.");
        }

        if (displayTerm.Trim().Length > 300)
        {
            throw new DomainException("Diagnosis display term is too long.");
        }

        if (note is not null && note.Length > 2000)
        {
            throw new DomainException("Note is too long.");
        }
    }
}