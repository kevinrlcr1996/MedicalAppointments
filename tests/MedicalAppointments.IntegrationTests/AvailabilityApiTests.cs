using MedicalAppointments.Contracts.Scheduling;
using System.Net.Http.Json;
using System.Globalization;

namespace MedicalAppointments.IntegrationTests;

[Collection("Api")]
public class AvailabilityApiTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public AvailabilityApiTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Availability_Should_Return_Slots_From_Schedule()
    {
        var token = await TestAuthHelper.GetSuperAdminTokenAsync(
            _fixture.Factory,
            _fixture.SuperAdminEmail,
            _fixture.SuperAdminPassword);

        var client = TestAuthHelper.CreateAuthorizedClient(_fixture.Factory, token);

        var doctors = await client.GetFromJsonAsync<PagedResponse<SpecialtyLike>>("/api/v1/doctors?page=1&pageSize=1");
        var clinics = await client.GetFromJsonAsync<PagedResponse<SpecialtyLike>>("/api/v1/clinics?page=1&pageSize=1");

        var doctorId = doctors!.Items[0].Id;
        var clinicId = clinics!.Items[0].Id;

        var date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7));

        var scheduleRequest = new CreateScheduleRequest(
            DoctorId: doctorId,
            ClinicId: clinicId,
            Name: $"Availability test {Guid.NewGuid():N}",
            IsDefault: true,
            IsActive: true,
            EffectiveFrom: date,
            EffectiveTo: null,
            SlotDurationMinutes: 30,
            BufferMinutes: 0,
            Days: [new ScheduleDayRequest(date.DayOfWeek, new TimeOnly(9, 0), new TimeOnly(10, 0))]);

        var createSchedule = await client.PostAsJsonAsync("/api/v1/doctor-schedules", scheduleRequest);
        createSchedule.EnsureSuccessStatusCode();

        var availability = await client.GetFromJsonAsync<AvailabilityDayResponse>(
            $"/api/v1/availability/doctors/{doctorId}/clinics/{clinicId}?date={date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");

        Assert.NotNull(availability);
        Assert.Equal(2, availability!.Slots.Count);
    }

    private sealed record SpecialtyLike(Guid Id);

    private sealed record PagedResponse<T>(
        IReadOnlyList<T> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);
}