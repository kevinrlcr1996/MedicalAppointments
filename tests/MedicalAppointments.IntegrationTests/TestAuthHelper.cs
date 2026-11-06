using System.Net.Http.Headers;
using System.Net.Http.Json;
using MedicalAppointments.Contracts.Auth;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MedicalAppointments.IntegrationTests;

public static class TestAuthHelper
{
    public static async Task<string> GetSuperAdminTokenAsync(
        WebApplicationFactory<Program> factory,
        string email,
        string password)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = email,
            Password = password
        });

        response.EnsureSuccessStatusCode();

        var auth = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();

        return auth!.AccessToken;
    }

    public static HttpClient CreateAuthorizedClient(
        WebApplicationFactory<Program> factory,
        string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }
}