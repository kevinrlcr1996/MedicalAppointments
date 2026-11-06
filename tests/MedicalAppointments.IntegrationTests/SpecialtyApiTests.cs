using System.Net;
using System.Net.Http.Json;
using MedicalAppointments.Contracts.MedicalStructure;
using Xunit;

namespace MedicalAppointments.IntegrationTests;

[Collection("Api")]
public class SpecialtyApiTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public SpecialtyApiTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Create_Specialty_Should_Work_For_SuperAdmin()
    {
        var token = await TestAuthHelper.GetSuperAdminTokenAsync(
            _fixture.Factory,
            _fixture.SuperAdminEmail,
            _fixture.SuperAdminPassword);

        var client = TestAuthHelper.CreateAuthorizedClient(_fixture.Factory, token);

        var name = $"Test Specialty {Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("/api/v1/specialties", new CreateSpecialtyRequest(
            Name: name,
            Code: null,
            Description: "Integration test",
            IsActive: true));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<SpecialtyResponse>();

        Assert.NotNull(created);
        Assert.Equal(name, created!.Name);

        var duplicateResponse = await client.PostAsJsonAsync("/api/v1/specialties", new CreateSpecialtyRequest(
            Name: name,
            Code: null,
            Description: null,
            IsActive: true));

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task Patient_Should_Not_Create_Specialty()
    {
        var client = _fixture.Factory.CreateClient();

        var email = $"patient-{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = email,
            Password = password,
            FirstName = "Patient",
            LastName = "Test"
        });

        Assert.Equal(HttpStatusCode.Accepted, register.StatusCode);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = email,
            Password = password
        });

        login.EnsureSuccessStatusCode();

        var auth = await login.Content.ReadFromJsonAsync<MedicalAppointments.Contracts.Auth.AuthenticationResponse>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var createResponse = await client.PostAsJsonAsync("/api/v1/specialties", new CreateSpecialtyRequest(
            Name: $"Should Fail {Guid.NewGuid():N}",
            Code: null,
            Description: null,
            IsActive: true));

        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
    }
}