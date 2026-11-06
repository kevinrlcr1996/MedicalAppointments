namespace MedicalAppointments.Application.Scheduling.Common;

public sealed record SchedulingClinicContext(
    Guid Id,
    Guid OrganizationId,
    string? TimeZoneId,
    bool IsActive);

public interface ISchedulingClinicLookup
{
    Task<SchedulingClinicContext?> GetContextAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default);
}