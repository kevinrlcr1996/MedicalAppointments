using FluentValidation;

namespace MedicalAppointments.Application.Reports;

public sealed class AppointmentReportQueryValidator : AbstractValidator<AppointmentReportQuery>
{
    public AppointmentReportQueryValidator()
    {
        RuleFor(x => x.FromDate).GreaterThan(DateOnly.MinValue);
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate);
        RuleFor(x => x.ToDate).Must(x => x < DateOnly.MaxValue)
            .WithMessage("ToDate must allow a following day for an exclusive UTC boundary.");
        RuleFor(x => x).Must(x => x.ToDate >= x.FromDate && x.ToDate.DayNumber - x.FromDate.DayNumber <= 365)
            .WithMessage("The report period cannot exceed 366 days.");
    }
}
