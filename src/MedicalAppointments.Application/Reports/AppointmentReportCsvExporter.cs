using System.Globalization;
using System.Text;

namespace MedicalAppointments.Application.Reports;

public static class AppointmentReportCsvExporter
{
    private static readonly string[] Header = ["Tipo", "Fecha", "Grupo", "Elemento", "Cantidad"];

    public static string Export(AppointmentReportDto report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var csv = new StringBuilder();
        AppendRow(csv, Header);
        AppendRow(csv, ["Resumen", "", "", "Período desde", report.FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)]);
        AppendRow(csv, ["Resumen", "", "", "Período hasta", report.ToDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)]);
        AppendRow(csv, ["Resumen", "", "", "Total de citas", Format(report.TotalAppointments)]);
        AppendRow(csv, ["Resumen", "", "", "Canceladas", Format(report.CancelledAppointments)]);
        AppendRow(csv, ["Resumen", "", "", "Ausencias", Format(report.NoShowAppointments)]);
        AppendRow(csv, ["Resumen", "", "", "Pacientes nuevos", Format(report.NewPatients)]);
        AppendRow(csv, ["Resumen", "", "", "Minutos disponibles", Format(report.AvailableMinutes)]);
        AppendRow(csv, ["Resumen", "", "", "Minutos reservados", Format(report.ReservedMinutes)]);
        AppendRow(csv, ["Resumen", "", "", "Ocupacion (%)", report.OccupancyPercentage?.ToString("0.0", CultureInfo.InvariantCulture) ?? ""]);
        AppendRow(csv, ["Resumen", "", "", "Solicitudes de cita", Format(report.AppointmentRequests)]);

        foreach (var item in report.ByStatus)
            AppendRow(csv, ["Estado", "", "", item.Status.ToString(), Format(item.Count)]);

        foreach (var item in report.ByDay)
            AppendRow(csv, ["Día UTC", item.DateUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "", "Citas", Format(item.Count)]);

        foreach (var item in report.ByDoctor)
            AppendRow(csv, ["Médico", "", "", item.Name, Format(item.Count)]);

        foreach (var item in report.BySpecialty)
            AppendRow(csv, ["Especialidad", "", "", item.Name, Format(item.Count)]);

        return csv.ToString();
    }

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static void AppendRow(StringBuilder csv, IEnumerable<string> values)
    {
        csv.AppendJoin(',', values.Select(EscapeCell));
        csv.Append("\r\n");
    }

    private static string EscapeCell(string value)
    {
        var firstMeaningfulCharacter = value.AsSpan().TrimStart();
        if (!firstMeaningfulCharacter.IsEmpty && firstMeaningfulCharacter[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            value = $"'{value}";

        if (value.IndexOfAny([',', '"', '\r', '\n']) < 0)
            return value;

        return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
