namespace MedicalAppointments.Contracts.Reports;

public sealed record AppointmentReportResponse(
    string FromDate,
    string ToDate,
    int TotalAppointments,
    int CancelledAppointments,
    int NoShowAppointments,
    int NewPatients,
    int AvailableMinutes,
    int ReservedMinutes,
    double? OccupancyPercentage,
    int AppointmentRequests,
    IReadOnlyList<AppointmentStatusReportResponse> ByStatus,
    IReadOnlyList<AppointmentDailyReportResponse> ByDay,
    IReadOnlyList<AppointmentGroupReportResponse> ByDoctor,
    IReadOnlyList<AppointmentGroupReportResponse> BySpecialty);

public sealed record AppointmentStatusReportResponse(string Status, int Count);
public sealed record AppointmentDailyReportResponse(string DateUtc, int Count);
public sealed record AppointmentGroupReportResponse(string Id, string Name, int Count);
