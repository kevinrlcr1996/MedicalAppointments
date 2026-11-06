using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Contracts.Auth;
using MedicalAppointments.Contracts.Foundation;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MedicalAppointments.IntegrationTests;

[Collection("Api")]
public class FoundationRealtimeTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public FoundationRealtimeTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Health_Live_Should_Return_Ok()
    {
        var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        response.EnsureSuccessStatusCode();
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SignalR_Should_Receive_Test_Event_After_Outbox_Processing()
    {
        var client = _fixture.Factory.CreateClient();

        var tokenResponse = await client.PostAsJsonAsync("/api/v1/dev/token", new DevTokenRequestDto("Integration Test", null));
        tokenResponse.EnsureSuccessStatusCode();

        var token = (await tokenResponse.Content.ReadFromJsonAsync<DevTokenResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            token.TokenType,
            token.AccessToken);

        var hubUrl = new Uri(client.BaseAddress!, "/hubs/foundation").ToString();

        await using var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token.AccessToken);
                options.HttpMessageHandlerFactory = _ => _fixture.Factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .WithAutomaticReconnect()
            .Build();

        var completion = new TaskCompletionSource<FoundationTestEventPayload>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        connection.On("TestEvent", (FoundationTestEventPayload payload) =>
        {
            completion.TrySetResult(payload);
        });

        await connection.StartAsync();
        await connection.InvokeAsync("JoinTestGroup");

        var publishResponse = await client.PostAsJsonAsync(
            "/api/v1/foundation/realtime/test",
            new PublishFoundationTestEventRequest("integration-event"));

        publishResponse.EnsureSuccessStatusCode();

        var winner = await Task.WhenAny(completion.Task, Task.Delay(TimeSpan.FromSeconds(20)));

        Assert.Same(completion.Task, winner);

        var payload = await completion.Task;

        Assert.Equal("integration-event", payload.Message);
    }
}