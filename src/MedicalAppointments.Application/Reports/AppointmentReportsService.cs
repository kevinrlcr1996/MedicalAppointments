using FluentValidation;
using MedicalAppointments.Application.Reports.Stores;
using MedicalAppointments.Domain.Appointments;

namespace MedicalAppointments.Application.Reports;

public sealed class AppointmentReportsService : IAppointmentReportsService
{
    private readonly IAppointmentReportsStore _store;
    private readonly IValidator<AppointmentReportQuery> _validator;

    public AppointmentReportsService(IAppointmentReportsStore store, IValidator<AppointmentReportQuery> validator)
    {
        _store = store;
        _validator = validator;
    }

    public async Task<AppointmentReportDto> GetAsync(AppointmentReportQuery query, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(query, cancellationToken);
        var fromUtc = new DateTimeOffset(query.FromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtcExclusive = new DateTimeOffset(query.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var data = await _store.GetAsync(fromUtc, toUtcExclusive, cancellationToken);
        var total = data.ByStatus.Sum(x => x.Count);

        return new AppointmentReportDto(
            query.FromDate,
            query.ToDate,
            total,
            data.ByStatus.Where(x => x.Status == AppointmentStatus.Cancelled).Sum(x => x.Count),
            data.ByStatus.Where(x => x.Status == AppointmentStatus.NoShow).Sum(x => x.Count),
            data.NewPatients,
            data.AvailableMinutes,
            data.ReservedMinutes,
            data.AppointmentRequests,
            data.ByStatus,
            data.ByDay,
            data.ByDoctor,
            data.BySpecialty);
    }
}
