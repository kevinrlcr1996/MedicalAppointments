using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Domain.ClinicalRecords;

public sealed class VitalSigns : AggregateRoot
{
    private VitalSigns()
    {
    }

    public Guid AppointmentId { get; private set; }

    public Guid AuthorUserId { get; private set; }

    public Guid AuthorDoctorId { get; private set; }

    public string AuthorDisplayName { get; private set; } = null!;

    public int? SystolicmmHg { get; private set; }
    public int? DiastolicmmHg { get; private set; }
    public int? HeartRateBpm { get; private set; }
    public decimal? TemperatureC { get; private set; }
    public VitalSignsTemperatureMethod? TemperatureMethod { get; private set; }
    public VitalSignsPosition? Position { get; private set; }
    public int? RespiratoryRatePerMin { get; private set; }
    public int? SpO2Percent { get; private set; }
    public decimal? WeightKg { get; private set; }
    public decimal? HeightCm { get; private set; }
    public int? PainScore0to10 { get; private set; }
    public string? Note { get; private set; }

    public bool IsFinal { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; }
    public DateTimeOffset? FinalizedAt { get; private set; }
    public Guid? FinalizedByUserId { get; private set; }
    public Guid? CorrectsVitalSignsId { get; private set; }

    // Navegaciones dentro del entity (rule 9). Direccionalidad rule 6:
    // el vital conoce la cita; la cita NO navega a vitales (Appointment intacto).
    public Appointment Appointment { get; private set; } = null!;
    public Doctor AuthorDoctor { get; private set; } = null!;
    public VitalSigns? CorrectsVitalSigns { get; private set; }

    public static VitalSigns Create(
        Guid appointmentId,
        Guid authorUserId,
        Guid authorDoctorId,
        string authorDisplayName,
        VitalSignsMeasurements m,
        DateTimeOffset recordedAtUtc,
        DateTimeOffset now)
    {
        RequireAppointment(appointmentId);
        RequireAuthor(authorUserId, authorDoctorId, authorDisplayName);
        Validate(m);

        return new VitalSigns
        {
            Id = Guid.CreateVersion7(),
            AppointmentId = appointmentId,
            AuthorUserId = authorUserId,
            AuthorDoctorId = authorDoctorId,
            AuthorDisplayName = authorDisplayName,
            SystolicmmHg = m.SystolicmmHg,
            DiastolicmmHg = m.DiastolicmmHg,
            HeartRateBpm = m.HeartRateBpm,
            TemperatureC = m.TemperatureC,
            TemperatureMethod = m.TemperatureMethod,
            Position = m.Position,
            RespiratoryRatePerMin = m.RespiratoryRatePerMin,
            SpO2Percent = m.SpO2Percent,
            WeightKg = m.WeightKg,
            HeightCm = m.HeightCm,
            PainScore0to10 = m.PainScore0to10,
            Note = NormalizeNote(m.Note),
            IsFinal = false,
            RecordedAtUtc = recordedAtUtc,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void UpdateDraft(VitalSignsMeasurements m, DateTimeOffset now)
    {
        if (IsFinal)
        {
            throw new DomainException("A finalized vital-sign record cannot be edited.");
        }

        Validate(m);

        SystolicmmHg = m.SystolicmmHg;
        DiastolicmmHg = m.DiastolicmmHg;
        HeartRateBpm = m.HeartRateBpm;
        TemperatureC = m.TemperatureC;
        TemperatureMethod = m.TemperatureMethod;
        Position = m.Position;
        RespiratoryRatePerMin = m.RespiratoryRatePerMin;
        SpO2Percent = m.SpO2Percent;
        WeightKg = m.WeightKg;
        HeightCm = m.HeightCm;
        PainScore0to10 = m.PainScore0to10;
        Note = NormalizeNote(m.Note);
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

    // Corrección aditiva (espejo de ClinicalNote.CreateCorrection): el original debe estar firmado;
    // nace como BORRADOR vinculado; el original queda intacto.
    public static VitalSigns CreateCorrection(
        VitalSigns original,
        Guid appointmentId,
        Guid authorUserId,
        Guid authorDoctorId,
        string authorDisplayName,
        VitalSignsMeasurements m,
        DateTimeOffset recordedAtUtc,
        DateTimeOffset now)
    {
        if (original is null)
        {
            throw new DomainException("Original vital-sign record is required.");
        }

        if (!original.IsFinal)
        {
            throw new DomainException("A correction can only target a finalized vital-sign record.");
        }

        RequireAppointment(appointmentId);
        RequireAuthor(authorUserId, authorDoctorId, authorDisplayName);
        Validate(m);

        return new VitalSigns
        {
            Id = Guid.CreateVersion7(),
            AppointmentId = appointmentId,
            AuthorUserId = authorUserId,
            AuthorDoctorId = authorDoctorId,
            AuthorDisplayName = authorDisplayName,
            SystolicmmHg = m.SystolicmmHg,
            DiastolicmmHg = m.DiastolicmmHg,
            HeartRateBpm = m.HeartRateBpm,
            TemperatureC = m.TemperatureC,
            TemperatureMethod = m.TemperatureMethod,
            Position = m.Position,
            RespiratoryRatePerMin = m.RespiratoryRatePerMin,
            SpO2Percent = m.SpO2Percent,
            WeightKg = m.WeightKg,
            HeightCm = m.HeightCm,
            PainScore0to10 = m.PainScore0to10,
            Note = NormalizeNote(m.Note),
            IsFinal = false,
            RecordedAtUtc = recordedAtUtc,
            CorrectsVitalSignsId = original.Id,
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

    private static string? NormalizeNote(string? note) =>
        string.IsNullOrWhiteSpace(note) ? null : note.Trim();

    // Guardas fisiológicas autocontenidas (sin referencia externa). Solo validan lo PRESENTE.
    private static void Validate(VitalSignsMeasurements m)
    {
        if (m.SystolicmmHg is var s and not null && (s < 50 || s > 300))
            throw new DomainException("Systolic must be between 50 and 300 mmHg.");
        if (m.DiastolicmmHg is var d and not null && (d < 30 || d > 200))
            throw new DomainException("Diastolic must be between 30 and 200 mmHg.");
        if (m.SystolicmmHg is not null && m.DiastolicmmHg is not null && m.SystolicmmHg <= m.DiastolicmmHg)
            throw new DomainException("Systolic must be greater than diastolic.");
        if (m.HeartRateBpm is var hr and not null && (hr < 20 || hr > 250))
            throw new DomainException("Heart rate must be between 20 and 250 bpm.");
        if (m.TemperatureC is var t and not null && (t < 30m || t > 45m))
            throw new DomainException("Temperature must be between 30 and 45 °C.");
        if (m.RespiratoryRatePerMin is var rr and not null && (rr < 4 || rr > 60))
            throw new DomainException("Respiratory rate must be between 4 and 60 /min.");
        if (m.SpO2Percent is var spo2 and not null && (spo2 < 50 || spo2 > 100))
            throw new DomainException("SpO2 must be between 50 and 100 %.");
        if (m.WeightKg is var w and not null && (w < 1m || w > 400m))
            throw new DomainException("Weight must be between 1 and 400 kg.");
        if (m.HeightCm is var h and not null && (h < 30m || h > 260m))
            throw new DomainException("Height must be between 30 and 260 cm.");
        if (m.PainScore0to10 is var p and not null && (p < 0 || p > 10))
            throw new DomainException("Pain score must be between 0 and 10.");
        if (m.Note is not null && m.Note.Length > 2000)
            throw new DomainException("Note is too long.");
    }
}