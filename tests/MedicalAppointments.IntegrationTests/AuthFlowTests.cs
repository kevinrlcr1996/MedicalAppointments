using MedicalAppointments.Contracts.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MedicalAppointments.IntegrationTests;

[Collection("Api")]
public class AuthFlowTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public AuthFlowTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Register_Login_Refresh_Logout_Should_Work()
    {
        var client = _fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        var email = $"user-{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = email,
            Password = password,
            FirstName = "Test",
            LastName = "User"
        });

        Assert.Equal(HttpStatusCode.Accepted, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = email,
            Password = password
        });

        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth!.AccessToken));
        Assert.Equal(email, auth.User.Email);

        var meResponse = await SendAuthorizedAsync(client, auth.AccessToken, "/api/v1/auth/me");
        meResponse.EnsureSuccessStatusCode();

        var refreshResponse = await client.PostAsync("/api/v1/auth/refresh", content: null);
        refreshResponse.EnsureSuccessStatusCode();

        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(refreshed);
        Assert.False(string.IsNullOrWhiteSpace(refreshed!.AccessToken));

        var logoutResponse = await client.PostAsync("/api/v1/auth/logout", content: null);
        logoutResponse.EnsureSuccessStatusCode();

        var afterLogoutRefresh = await client.PostAsync("/api/v1/auth/refresh", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogoutRefresh.StatusCode);
    }

    [Fact]
    public async Task Refresh_Token_Reuse_Should_Revoke_Family()
    {
        var client = _fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = false
        });

        var email = $"reuse-{Guid.NewGuid():N}@example.com";
        var password = "Password123!";

        await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = email,
            Password = password,
            FirstName = "Reuse",
            LastName = "Test"
        });

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = email,
            Password = password
        });

        loginResponse.EnsureSuccessStatusCode();

        var oldCookie = ExtractCookie(loginResponse, "MA_REFRESH_TOKEN");
        Assert.False(string.IsNullOrWhiteSpace(oldCookie));

        var firstRefresh = await client.PostAsync("/api/v1/auth/refresh", content: null);
        firstRefresh.EnsureSuccessStatusCode();

        using var reuseClient = _fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            AllowAutoRedirect = false
        });

        using var reuseRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        reuseRequest.Headers.Add("Cookie", $"MA_REFRESH_TOKEN={oldCookie}");

        var reuseResponse = await reuseClient.SendAsync(reuseRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);

        var secondRefresh = await client.PostAsync("/api/v1/auth/refresh", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, secondRefresh.StatusCode);
    }

    private static async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpClient client,
        string token,
        string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await client.SendAsync(request);
    }

    private static string? ExtractCookie(HttpResponseMessage response, string cookieName)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return null;
        }

        foreach (var value in values)
        {
            var parts = value.Split(';');
            var nameValue = parts[0].Split('=', 2);

            if (nameValue.Length == 2 && nameValue[0].Trim() == cookieName)
            {
                return nameValue[1].Trim();
            }
        }

        return null;
    }
}