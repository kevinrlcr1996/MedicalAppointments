using MedicalAppointments.Application.Auditing;

namespace MedicalAppointments.UnitTests.Application;

public sealed class AuditLogQueryValidatorTests
{
    private readonly AuditLogQueryValidator _validator = new();

    [Fact]
    public async Task Accepts_valid_paged_query_with_a_period_of_at_most_366_days()
    {
        var result = await _validator.ValidateAsync(new AuditLogQuery(
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), Page: 1, PageSize: 100));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Rejects_inverted_or_excessively_wide_date_periods()
    {
        var inverted = await _validator.ValidateAsync(new AuditLogQuery(new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1)));
        var tooWide = await _validator.ValidateAsync(new AuditLogQuery(new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 2)));

        Assert.False(inverted.IsValid);
        Assert.False(tooWide.IsValid);
    }

    [Fact]
    public async Task Rejects_page_sizes_above_one_hundred()
    {
        var result = await _validator.ValidateAsync(new AuditLogQuery(
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), PageSize: 101));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Rejects_values_that_could_overflow_pagination_or_date_arithmetic()
    {
        var result = await _validator.ValidateAsync(new AuditLogQuery(
            new DateOnly(2026, 1, 1), DateOnly.MaxValue, Page: int.MaxValue));

        Assert.False(result.IsValid);
    }
}
