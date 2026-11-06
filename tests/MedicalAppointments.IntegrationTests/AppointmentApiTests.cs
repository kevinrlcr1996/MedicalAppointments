using System.Net.Http.Json;
using MedicalAppointments.Contracts.Scheduling;

namespace MedicalAppointments.IntegrationTests;

[Collection("Api")]
public class AppointmentApiTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public AppointmentApiTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Create_Appointment_With_Same_Idempotency_Key_Should_Not_Duplicate()
    {
        var token = await TestAuthHelper.GetSuperAdminTokenAsync(
            _fixture.Factory,
            _fixture.SuperAdminEmail,
            _fixture.SuperAdminPassword);

        var client = TestAuthHelper.CreateAuthorizedClient(_fixture.Factory, token);

        var patients = await client.GetFromJsonAsync<PagedResponse<IdOnly>>(
            "/api/v1/patients?page=1&pageSize=1");
        var doctors = await client.GetFromJsonAsync<PagedResponse<IdOnly>>(
            "/api/v1/doctors?page=1&pageSize=1");
        var clinics = await client.GetFromJsonAsync<PagedResponse<IdOnly>>(
            "/api/v1/clinics?page=1&pageSize=1&isActive=true");

        var patientId = patients!.Items[0].Id;
        var doctorId = doctors!.Items[0].Id;
        var clinicId = clinics!.Items[0].Id;
        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

        var scheduleResponse = await client.PostAsJsonAsync(
            "/api/v1/doctor-schedules",
            new CreateScheduleRequest(
                doctorId,
                clinicId,
                $"Appointment test {Guid.NewGuid():N}",
                true,
                true,
                date,
                null,
                30,
                0,
                [new ScheduleDayRequest(date.DayOfWeek, new TimeOnly(9, 0), new TimeOnly(10, 0))]));
        scheduleResponse.EnsureSuccessStatusCode();

        var availability = await client.GetFromJsonAsync<AvailabilityDayResponse>(
            $"/api/v1/availability/doctors/{doctorId}/clinics/{clinicId}?date={date:yyyy-MM-dd}");
        var slot = availability!.Slots[0];
        var idempotencyKey = Guid.NewGuid().ToString();

        HttpRequestMessage CreateRequest(string key)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/appointments");
            request.Headers.Add("Idempotency-Key", key);
            request.Content = JsonContent.Create(new
            {
                PatientId = patientId,
                DoctorId = doctorId,
                ClinicId = clinicId,
                RoomId = (Guid?)null,
                LocalDate = date,
                StartAtUtc = slot.StartAtUtc,
                EndAtUtc = slot.EndAtUtc,
                Reason = "Test",
                Notes = (string?)null
            });

            return request;
        }

        var first = await client.SendAsync(CreateRequest(idempotencyKey));
        var second = await client.SendAsync(CreateRequest(idempotencyKey));

        Assert.Equal(System.Net.HttpStatusCode.Created, first.StatusCode);
        Assert.True(first.IsSuccessStatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, second.StatusCode);
    }

    private sealed record IdOnly(Guid Id);

    private sealed record PagedResponse<T>(
        IReadOnlyList<T> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);
}