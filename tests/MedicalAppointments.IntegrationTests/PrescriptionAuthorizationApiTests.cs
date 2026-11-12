using System.Net;
using System.Net.Http.Json;
using MedicalAppointments.Contracts.ClinicalRecords;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalAppointments.IntegrationTests;

[Collection("Api")]
public sealed class PrescriptionAuthorizationApiTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public PrescriptionAuthorizationApiTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Prescription_write_requires_a_permission_policy()
    {
        using var client = _fixture.Factory.CreateClient();
        var email = $"patient-{Guid.NewGuid():N}@example.com";
        const string password = "Password123!";

        var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = email,
            Password = password,
            FirstName = "Patient",
            LastName = "Without prescription permission"
        });
        Assert.Equal(HttpStatusCode.Accepted, registration.StatusCode);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { Email = email, Password = password });
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<MedicalAppointments.Contracts.Auth.AuthenticationResponse>();
        Assert.NotNull(auth);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var patientId = await GetSeededPatientIdAsync();
        var response = await client.PostAsJsonAsync($"/api/v1/clinical-records/patients/{patientId}/prescriptions", ValidPrescription());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Authorized_staff_without_doctor_profile_gets_profile_conflict_not_forbidden()
    {
        var token = await TestAuthHelper.GetSuperAdminTokenAsync(
            _fixture.Factory, _fixture.SuperAdminEmail, _fixture.SuperAdminPassword);
        using var client = TestAuthHelper.CreateAuthorizedClient(_fixture.Factory, token);
        var patientId = await GetSeededPatientIdAsync();

        var response = await client.PostAsJsonAsync($"/api/v1/clinical-records/patients/{patientId}/prescriptions", ValidPrescription());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.Contains("doctor profile", problem?.Detail, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<Guid> GetSeededPatientIdAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Patients.Select(x => x.Id).FirstAsync();
    }

    private static CreatePrescriptionRequest ValidPrescription() => new(
        MedicationName: "Integration test medication",
        Strength: null,
        DosageForm: null,
        Frequency: "Once daily",
        Route: "Oral",
        Quantity: 7,
        RefillsAllowed: 0,
        DaysSupply: 7,
        Instructions: null,
        Note: null,
        RecordedAtUtc: DateTimeOffset.UtcNow);
}
