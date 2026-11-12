namespace MedicalAppointments.Application.Reports.Stores;

public interface IAppointmentReportsStore
{
    Task<AppointmentReportData> GetAsync(DateTimeOffset fromUtc, DateTimeOffset toUtcExclusive, CancellationToken cancellationToken = default);
}
