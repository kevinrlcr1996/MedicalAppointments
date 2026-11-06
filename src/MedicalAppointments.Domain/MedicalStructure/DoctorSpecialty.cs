namespace MedicalAppointments.Domain.MedicalStructure;

public sealed class DoctorSpecialty
{
    private DoctorSpecialty()
    {
    }

    public DoctorSpecialty(Guid doctorId, Guid specialtyId, DateTimeOffset assignedAt)
    {
        DoctorId = doctorId;
        SpecialtyId = specialtyId;
        AssignedAt = assignedAt;
    }

    public Guid DoctorId { get; private set; }

    public Guid SpecialtyId { get; private set; }

    public DateTimeOffset AssignedAt { get; private set; }

    public Doctor Doctor { get; private set; } = null!;

    public Specialty Specialty { get; private set; } = null!;
}