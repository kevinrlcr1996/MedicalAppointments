using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Text;
using Testcontainers.MsSql;
using Xunit;

namespace MedicalAppointments.IntegrationTests;

public sealed class ApiTestFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithDatabase("MedicalAppointments")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public string ConnectionString { get; private set; } = null!;

    public string SuperAdminEmail { get; } = "superadmin@medicalappointments.local";

    public string SuperAdminPassword { get; } = "Integration_test_123!";

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        ConnectionString = $"{_sqlContainer.GetConnectionString()};TrustServerCertificate=True";

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(Environments.Development);

                builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
                builder.UseSetting("Auth:Jwt:Key", new string('a', 64));
                builder.UseSetting("Auth:Jwt:Issuer", "MedicalAppointments");
                builder.UseSetting("Auth:Jwt:Audience", "MedicalAppointments");
                builder.UseSetting("Auth:Email:RequireConfirmation", "false");
                builder.UseSetting("Auth:RefreshToken:CookieSecure", "false");
                builder.UseSetting("Outbox:PollingIntervalSeconds", "1");
                builder.UseSetting("Outbox:BatchSize", "10");
                builder.UseSetting("Seed:Enabled", "true");
                builder.UseSetting("Seed:DemoData", "true");
                builder.UseSetting("Seed:SuperAdminEmail", SuperAdminEmail);
                builder.UseSetting("Seed:SuperAdminPassword", SuperAdminPassword);
                builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:5173");
                var clamAvEnabled = string.Equals(Environment.GetEnvironmentVariable("MA_TEST_CLAMAV_ENABLED"), "true", StringComparison.OrdinalIgnoreCase);
                builder.UseSetting("Attachments:Scanner:Enabled", clamAvEnabled.ToString());
                builder.UseSetting("Attachments:Scanner:ClamAvHost", Environment.GetEnvironmentVariable("MA_TEST_CLAMAV_HOST" ) ?? "127.0.0.1" );
                builder.UseSetting("Attachments:Scanner:ClamAvPort", Environment.GetEnvironmentVariable("MA_TEST_CLAMAV_PORT" ) ?? "3310" );
                if (clamAvEnabled)
                {
                    builder.ConfigureTestServices(services =>
                    {
                        services.RemoveAll<MedicalAppointments.Application.ClinicalRecords.Storage.IFileStorage>();
                        services.AddSingleton<InMemoryTestFileStorage>();
                        services.AddSingleton<MedicalAppointments.Application.ClinicalRecords.Storage.IFileStorage>(provider => provider.GetRequiredService<InMemoryTestFileStorage>());
                    });
                }
            });

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
        {
            await Factory.DisposeAsync();
        }

        if (_sqlContainer is not null)
        {
            await _sqlContainer.DisposeAsync();
        }
    }
}
