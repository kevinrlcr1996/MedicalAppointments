using MedicalAppointments.Application.Idempotency;
using MedicalAppointments.Domain.Idempotency;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalAppointments.IntegrationTests;

[Collection("Api")]
public sealed class IdempotencyCleanupApiTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public IdempotencyCleanupApiTests(ApiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task DeleteExpiredAsync_removes_only_expired_records_and_respects_batch_size()
    {
        var now = DateTimeOffset.UtcNow;
        var expiredOne = IdempotencyRecord.Create($"cleanup-{Guid.NewGuid()}", "test", "hash", null, now.AddDays(-2), TimeSpan.FromHours(1));
        var expiredTwo = IdempotencyRecord.Create($"cleanup-{Guid.NewGuid()}", "test", "hash", null, now.AddDays(-1), TimeSpan.FromHours(1));
        var active = IdempotencyRecord.Create($"cleanup-{Guid.NewGuid()}", "test", "hash", null, now, TimeSpan.FromDays(1));
        var ids = new[] { expiredOne.Id, expiredTwo.Id, active.Id };

        using (var seedScope = _fixture.Factory.Services.CreateScope())
        {
            var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.IdempotencyRecords.AddRange(expiredOne, expiredTwo, active);
            await db.SaveChangesAsync();
        }

        try
        {
            using var cleanupScope = _fixture.Factory.Services.CreateScope();
            var store = cleanupScope.ServiceProvider.GetRequiredService<IIdempotencyStore>();

            var deleted = await store.DeleteExpiredAsync(now, batchSize: 1);

            Assert.Equal(1, deleted);

            var remaining = await cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>()
                .IdempotencyRecords
                .Where(x => ids.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync();

            Assert.Equal(2, remaining.Count);
            Assert.Contains(active.Id, remaining);
            Assert.Contains(expiredTwo.Id, remaining);
            Assert.DoesNotContain(expiredOne.Id, remaining);
        }
        finally
        {
            using var cleanupScope = _fixture.Factory.Services.CreateScope();
            var db = cleanupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.IdempotencyRecords.Where(x => ids.Contains(x.Id)).ExecuteDeleteAsync();
        }
    }
}
