using Asp.Versioning;
using System.Text;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.Reports;
using MedicalAppointments.Contracts.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointments.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/reports")]
[ApiVersion("1.0")]
[Authorize(Policy = Permissions.ReportsRead)]
public sealed class ReportsController : ControllerBase
{
    private readonly IAppointmentReportsService _reports;

    public ReportsController(IAppointmentReportsService reports) => _reports = reports;

    [HttpGet("appointments")]
    [ProducesResponseType(typeof(AppointmentReportResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AppointmentReportResponse>> GetAppointments(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var report = await _reports.GetAsync(new AppointmentReportQuery(fromDate, toDate), cancellationToken);
        return Ok(new AppointmentReportResponse(
            report.FromDate.ToString("yyyy-MM-dd"),
            report.ToDate.ToString("yyyy-MM-dd"),
            report.TotalAppointments,
            report.CancelledAppointments,
            report.NoShowAppointments,
            report.NewPatients,
            report.AvailableMinutes,
            report.ReservedMinutes,
            report.OccupancyPercentage,
            report.AppointmentRequests,
            report.ByStatus.Select(x => new AppointmentStatusReportResponse(x.Status.ToString(), x.Count)).ToArray(),
            report.ByDay.Select(x => new AppointmentDailyReportResponse(x.DateUtc.ToString("yyyy-MM-dd"), x.Count)).ToArray(),
            report.ByDoctor.Select(x => new AppointmentGroupReportResponse(x.Id.ToString(), x.Name, x.Count)).ToArray(),
            report.BySpecialty.Select(x => new AppointmentGroupReportResponse(x.Id.ToString(), x.Name, x.Count)).ToArray()));
    }

    [HttpGet("appointments/export")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportAppointments(
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var report = await _reports.GetAsync(new AppointmentReportQuery(fromDate, toDate), cancellationToken);
        var csv = AppointmentReportCsvExporter.Export(report);
        var content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        return File(content, "text/csv; charset=utf-8", $"citas-{fromDate:yyyyMMdd}-{toDate:yyyyMMdd}.csv");
    }
}
