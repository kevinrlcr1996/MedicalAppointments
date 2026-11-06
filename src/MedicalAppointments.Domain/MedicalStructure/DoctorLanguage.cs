namespace MedicalAppointments.Domain.MedicalStructure;

public sealed class DoctorLanguage
{
    private DoctorLanguage()
    {
    }

    public DoctorLanguage(Guid doctorId, string language, DateTimeOffset addedAt)
    {
        DoctorId = doctorId;
        Language = language;
        AddedAt = addedAt;
    }

    public Guid DoctorId { get; private set; }

    public string Language { get; private set; } = null!;

    public DateTimeOffset AddedAt { get; private set; }

    public Doctor Doctor { get; private set; } = null!;
}