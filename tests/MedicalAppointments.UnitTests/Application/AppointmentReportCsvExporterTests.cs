using MedicalAppointments.Application.Reports;
using MedicalAppointments.Domain.Appointments;

namespace MedicalAppointments.UnitTests.Application;

public sealed class AppointmentReportCsvExporterTests
{
    [Fact]
    public void Occupancy_is_reserved_minutes_over_available_minutes_and_is_null_when_no_agenda_exists()
    {
        var report = Report();

        Assert.Equal(15, report.OccupancyPercentage);
        Assert.Null((report with { AvailableMinutes = 0 }).OccupancyPercentage);
    }

    [Fact]
    public void Exports_summary_and_all_aggregate_sections()
    {
        var report = Report();

        var csv = AppointmentReportCsvExporter.Export(report);

        Assert.Contains("Resumen,,,Total de citas,4\r\n", csv);
        Assert.Contains("Resumen,,,Minutos disponibles,600\r\n", csv);
        Assert.Contains("Resumen,,,Minutos reservados,90\r\n", csv);
        Assert.Contains("Resumen,,,Ocupacion (%),15.0\r\n", csv);
        Assert.Contains("Resumen,,,Solicitudes de cita,7\r\n", csv);
        Assert.Contains("Estado,,,Completed,2\r\n", csv);
        Assert.Contains("Día UTC,2026-11-12,,Citas,4\r\n", csv);
        Assert.Contains("Médico,,,Dra. Ejemplo,3\r\n", csv);
        Assert.Contains("Especialidad,,,Cardiología,3\r\n", csv);
    }

    [Fact]
    public void Escapes_csv_values_and_neutralizes_spreadsheet_formulas()
    {
        var report = Report() with
        {
            ByDoctor = [new AppointmentGroupCount(Guid.NewGuid(), "=HYPERLINK(\"https://example.com\",\"clic\")", 1)]
        };

        var csv = AppointmentReportCsvExporter.Export(report);

        Assert.Contains("\"'=HYPERLINK(\"\"https://example.com\"\",\"\"clic\"\")\"", csv);
    }

    private static AppointmentReportDto Report() => new(
        new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), 4, 1, 0, 2, 600, 90, 7,
        [new AppointmentStatusCount(AppointmentStatus.Completed, 2)],
        [new AppointmentDailyCount(new DateOnly(2026, 11, 12), 4)],
        [new AppointmentGroupCount(Guid.NewGuid(), "Dra. Ejemplo", 3)],
        [new AppointmentGroupCount(Guid.NewGuid(), "Cardiología", 3)]);
}
