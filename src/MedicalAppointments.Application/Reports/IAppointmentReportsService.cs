namespace MedicalAppointments.Application.Reports;

public interface IAppointmentReportsService
{
    Task<AppointmentReportDto> GetAsync(AppointmentReportQuery query, CancellationToken cancellationToken = default);
}
