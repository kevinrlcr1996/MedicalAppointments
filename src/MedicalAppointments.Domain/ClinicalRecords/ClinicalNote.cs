using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Domain.ClinicalRecords;

public sealed class ClinicalNote : AggregateRoot
{
    private ClinicalNote()
    {
    }

    public Guid AppointmentId { get; private set; }

    public Guid AuthorUserId { get; private set; }

    public Guid AuthorDoctorId { get; private set; }

    public string AuthorDisplayName { get; private set; } = null!;

    public ClinicalNoteCategory Category { get; private set; }

    public string Title { get; private set; } = null!;

    public string Content { get; private set; } = null!;

    public bool IsFinal { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public DateTimeOffset? FinalizedAt { get; private set; }

    public Guid? FinalizedByUserId { get; private set; }

    public Guid? CorrectsNoteId { get; private set; }

    public Appointment Appointment { get; private set; } = null!;

    public Doctor AuthorDoctor { get; private set; } = null!;

    public ClinicalNote? CorrectsNote { get; private set; }

    public static ClinicalNote Create(
        Guid appointmentId,
        Guid authorUserId,
        Guid authorDoctorId,
        string authorDisplayName,
        ClinicalNoteCategory category,
        string title,
        string content,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset now)
    {
        if (appointmentId == Guid.Empty)
        {
            throw new DomainException("Appointment is required.");
        }

        if (authorUserId == Guid.Empty)
        {
            throw new DomainException("Author user is required.");
        }

        if (authorDoctorId == Guid.Empty)
        {
            throw new DomainException("Author must have a doctor profile.");
        }

        ValidateText(title, content);

        return new ClinicalNote
        {
            Id = Guid.CreateVersion7(),
            AppointmentId = appointmentId,
            AuthorUserId = authorUserId,
            AuthorDoctorId = authorDoctorId,
            AuthorDisplayName = authorDisplayName.Trim(),
            Category = category,
            Title = title.Trim(),
            Content = content.Trim(),
            IsFinal = false,
            OccurredAtUtc = occurredAtUtc,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static ClinicalNote CreateCorrection(
        ClinicalNote original,
        Guid authorUserId,
        Guid authorDoctorId,
        string authorDisplayName,
        ClinicalNoteCategory category,
        string title,
        string content,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset now)
    {
        if (original is null)
        {
            throw new DomainException("Original note is required.");
        }

        if (!original.IsFinal)
        {
            throw new DomainException("Only a finalized note can be corrected.");
        }

        var correction = Create(
            original.AppointmentId,
            authorUserId,
            authorDoctorId,
            authorDisplayName,
            category,
            title,
            content,
            occurredAtUtc,
            now);

        correction.CorrectsNoteId = original.Id;

        return correction;
    }

    public void UpdateDraft(ClinicalNoteCategory category, string title, string content, DateTimeOffset now)
    {
        if (IsFinal)
        {
            throw new DomainException("A finalized note cannot be edited. Create a correction instead.");
        }

        ValidateText(title, content);

        Category = category;
        Title = title.Trim();
        Content = content.Trim();

        Touch(now);
    }

    public void Finalize(Guid finalizedByUserId, DateTimeOffset now)
    {
        if (IsFinal)
        {
            throw new DomainException("Note is already finalized.");
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

    private static void ValidateText(string title, string content)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainException("Content is required.");
        }
    }
}