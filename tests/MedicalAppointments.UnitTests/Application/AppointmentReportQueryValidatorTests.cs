using MedicalAppointments.Application.Reports;

namespace MedicalAppointments.UnitTests.Application;

public sealed class AppointmentReportQueryValidatorTests
{
    private readonly AppointmentReportQueryValidator _validator = new();

    [Fact]
    public async Task Accepts_a_valid_period_up_to_366_days()
    {
        var result = await _validator.ValidateAsync(new AppointmentReportQuery(
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Rejects_a_period_longer_than_366_days()
    {
        var result = await _validator.ValidateAsync(new AppointmentReportQuery(
            new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 2)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("366 days", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Rejects_a_start_date_after_the_end_date()
    {
        var result = await _validator.ValidateAsync(new AppointmentReportQuery(
            new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1)));

        Assert.False(result.IsValid);
    }
}
