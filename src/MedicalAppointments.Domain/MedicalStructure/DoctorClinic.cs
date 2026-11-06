namespace MedicalAppointments.Domain.MedicalStructure;

public sealed class DoctorClinic
{
    private DoctorClinic()
    {
    }

    public DoctorClinic(
        Guid doctorId,
        Guid clinicId,
        bool isPrimary,
        DateOnly? startDate,
        DateOnly? endDate,
        DateTimeOffset assignedAt)
    {
        DoctorId = doctorId;
        ClinicId = clinicId;
        IsPrimary = isPrimary;
        StartDate = startDate;
        EndDate = endDate;
        AssignedAt = assignedAt;
    }

    public Guid DoctorId { get; private set; }

    public Guid ClinicId { get; private set; }

    public bool IsPrimary { get; private set; }

    public DateOnly? StartDate { get; private set; }

    public DateOnly? EndDate { get; private set; }

    public DateTimeOffset AssignedAt { get; private set; }

    public Doctor Doctor { get; private set; } = null!;

    public Clinic Clinic { get; private set; } = null!;
}