using MedicalAppointments.Domain.Appointments;

namespace MedicalAppointments.Application.Reports;

public sealed record AppointmentReportQuery(DateOnly FromDate, DateOnly ToDate);

public sealed record AppointmentReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int TotalAppointments,
    int CancelledAppointments,
    int NoShowAppointments,
    int NewPatients,
    int AvailableMinutes,
    int ReservedMinutes,
    int AppointmentRequests,
    IReadOnlyList<AppointmentStatusCount> ByStatus,
    IReadOnlyList<AppointmentDailyCount> ByDay,
    IReadOnlyList<AppointmentGroupCount> ByDoctor,
    IReadOnlyList<AppointmentGroupCount> BySpecialty)
{
    public double? OccupancyPercentage => AvailableMinutes <= 0 ? null : Math.Round(ReservedMinutes * 100d / AvailableMinutes, 1);
}

public sealed record AppointmentStatusCount(AppointmentStatus Status, int Count);
public sealed record AppointmentDailyCount(DateOnly DateUtc, int Count);
public sealed record AppointmentGroupCount(Guid Id, string Name, int Count);

public sealed record AppointmentReportData(
    IReadOnlyList<AppointmentStatusCount> ByStatus,
    IReadOnlyList<AppointmentDailyCount> ByDay,
    IReadOnlyList<AppointmentGroupCount> ByDoctor,
    IReadOnlyList<AppointmentGroupCount> BySpecialty,
    int NewPatients,
    int AvailableMinutes,
    int ReservedMinutes,
    int AppointmentRequests);
