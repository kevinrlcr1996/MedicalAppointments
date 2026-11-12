using System.Net;
using System.Net.Http.Json;
using MedicalAppointments.Contracts.Reports;
using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalAppointments.IntegrationTests;

[Collection("Api")]
public sealed class AppointmentReportsApiTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public AppointmentReportsApiTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Requires_reports_permission()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync($"/api/v1/reports/appointments?fromDate={DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}&toDate={DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Exports_the_appointment_report_as_csv_for_authorized_users()
    {
        var token = await TestAuthHelper.GetSuperAdminTokenAsync(
            _fixture.Factory, _fixture.SuperAdminEmail, _fixture.SuperAdminPassword);
        using var client = TestAuthHelper.CreateAuthorizedClient(_fixture.Factory, token);
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/v1/reports/appointments/export?fromDate={today}&toDate={today}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
        Assert.Contains("Tipo,Fecha,Grupo,Elemento,Cantidad", System.Text.Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public async Task Returns_aggregated_counts_for_the_selected_period()
    {
        var fromDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var fromUtc = new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var appointmentId = Guid.Empty;

        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var patientId = await db.Patients.Select(x => x.Id).FirstAsync();
            var doctorId = await db.Doctors.Select(x => x.Id).FirstAsync();
            var clinicId = await db.Clinics.Select(x => x.Id).FirstAsync();
            var now = DateTimeOffset.UtcNow;
            var appointment = Appointment.Create(patientId, doctorId, clinicId, null, fromDate,
                fromUtc.AddHours(1), fromUtc.AddHours(1).AddMinutes(30), "Report integration test", null, null, now);
            appointmentId = appointment.Id;
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
        }

        var token = await TestAuthHelper.GetSuperAdminTokenAsync(
            _fixture.Factory, _fixture.SuperAdminEmail, _fixture.SuperAdminPassword);
        using var client = TestAuthHelper.CreateAuthorizedClient(_fixture.Factory, token);
        var response = await client.GetAsync($"/api/v1/reports/appointments?fromDate={fromDate:yyyy-MM-dd}&toDate={fromDate:yyyy-MM-dd}");

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var report = await response.Content.ReadFromJsonAsync<AppointmentReportResponse>();
        Assert.NotNull(report);
        Assert.Equal(fromDate.ToString("yyyy-MM-dd"), report!.FromDate);
        Assert.Contains(report.ByStatus, x => x.Status == "Pending" && x.Count > 0);
        Assert.Contains(report.ByDoctor, x => x.Count > 0);
        Assert.Contains(report.ByDay, x => x.DateUtc == fromDate.ToString("yyyy-MM-dd") && x.Count > 0);
        Assert.True(report.ReservedMinutes >= 30);
        Assert.True(report.AppointmentRequests >= 1);
        Assert.True(report.AvailableMinutes >= 0);
        Assert.True(report.OccupancyPercentage is null or >= 0);

        using var cleanupScope = _fixture.Factory.Services.CreateScope();
        var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var inserted = await cleanupDb.Appointments.FirstOrDefaultAsync(x => x.Id == appointmentId);
        if (inserted is not null)
        {
            cleanupDb.Appointments.Remove(inserted);
            await cleanupDb.SaveChangesAsync();
        }
    }
}
