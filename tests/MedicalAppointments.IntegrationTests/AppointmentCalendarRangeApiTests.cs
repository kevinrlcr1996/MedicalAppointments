using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MedicalAppointments.Contracts.Appointments;
using MedicalAppointments.Contracts.Common;
using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalAppointments.IntegrationTests;

[Collection("Api")]
public sealed class AppointmentCalendarRangeApiTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public AppointmentCalendarRangeApiTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Appointment_list_respects_utc_range_used_by_calendar()
    {
        var now = DateTimeOffset.UtcNow;
        var startAt = new DateTimeOffset(DateTime.UtcNow.AddDays(3).Date.AddHours(14), TimeSpan.Zero);
        var appointmentId = Guid.Empty;
        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var patientId = await db.Patients.Select(x => x.Id).FirstAsync();
            var doctorId = await db.Doctors.Select(x => x.Id).FirstAsync();
            var clinicId = await db.Clinics.Select(x => x.Id).FirstAsync();
            var appointment = Appointment.Create(patientId, doctorId, clinicId, null,
                DateOnly.FromDateTime(startAt.UtcDateTime), startAt, startAt.AddMinutes(30),
                "Calendar range test", null, null, now);
            appointmentId = appointment.Id;
            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();
        }

        try
        {
            var token = await TestAuthHelper.GetSuperAdminTokenAsync(
                _fixture.Factory, _fixture.SuperAdminEmail, _fixture.SuperAdminPassword);
            using var client = TestAuthHelper.CreateAuthorizedClient(_fixture.Factory, token);
            static string Utc(DateTimeOffset value) => Uri.EscapeDataString(value.ToString("O"));
            var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                Converters = { new JsonStringEnumConverter() }
            };

            var inRange = await client.GetFromJsonAsync<PaginatedResponse<AppointmentResponse>>(
                $"/api/v1/appointments?fromUtc={Utc(startAt.AddMinutes(-15))}&toUtc={Utc(startAt.AddMinutes(45))}&page=1&pageSize=100",
                jsonOptions);
            var outsideRange = await client.GetFromJsonAsync<PaginatedResponse<AppointmentResponse>>(
                $"/api/v1/appointments?fromUtc={Utc(startAt.AddHours(1))}&toUtc={Utc(startAt.AddHours(2))}&page=1&pageSize=100",
                jsonOptions);

            Assert.NotNull(inRange);
            Assert.Contains(inRange!.Items, x => x.Id == appointmentId);
            Assert.NotNull(outsideRange);
            Assert.DoesNotContain(outsideRange!.Items, x => x.Id == appointmentId);
        }
        finally
        {
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
}
