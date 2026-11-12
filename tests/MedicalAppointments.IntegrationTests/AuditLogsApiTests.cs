using System.Net;
using System.Net.Http.Json;
using MedicalAppointments.Contracts.Auditing;
using MedicalAppointments.Contracts.Common;
using MedicalAppointments.Domain.Auditing;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalAppointments.IntegrationTests;

[Collection("Api")]
public sealed class AuditLogsApiTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public AuditLogsApiTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Requires_audit_permission()
    {
        using var client = _fixture.Factory.CreateClient();
        var date = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        var response = await client.GetAsync($"/api/v1/audit-logs?fromDate={date}&toDate={date}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Returns_metadata_only_and_supports_filtered_pagination()
    {
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        const string privateClinicalValue = "private-clinical-value-do-not-return";
        var audit = AuditLog.Create(null, "Test.AuditRead", "TestRecord", Guid.NewGuid(), now,
            "192.0.2.1", "test-agent", null, $"{{\"clinicalValue\":\"{privateClinicalValue}\"}}", "audit-test-correlation");

        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AuditLogs.Add(audit);
            await db.SaveChangesAsync();
        }

        try
        {
            var token = await TestAuthHelper.GetSuperAdminTokenAsync(
                _fixture.Factory, _fixture.SuperAdminEmail, _fixture.SuperAdminPassword);
            using var client = TestAuthHelper.CreateAuthorizedClient(_fixture.Factory, token);
            var url = $"/api/v1/audit-logs?fromDate={today:yyyy-MM-dd}&toDate={today:yyyy-MM-dd}&action=Test.AuditRead&page=1&pageSize=10";

            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var page = await response.Content.ReadFromJsonAsync<PaginatedResponse<AuditLogResponse>>();
            Assert.NotNull(page);
            var item = Assert.Single(page!.Items);
            Assert.Equal(audit.Id, item.Id);
            Assert.Equal("Test.AuditRead", item.Action);

            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain(privateClinicalValue, body, StringComparison.Ordinal);
            Assert.DoesNotContain("oldValues", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("newValues", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ipAddress", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("userAgent", body, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            using var cleanupScope = _fixture.Factory.Services.CreateScope();
            var cleanupDb = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var inserted = await cleanupDb.AuditLogs.FirstOrDefaultAsync(x => x.Id == audit.Id);
            if (inserted is not null)
            {
                cleanupDb.AuditLogs.Remove(inserted);
                await cleanupDb.SaveChangesAsync();
            }
        }
    }
}
