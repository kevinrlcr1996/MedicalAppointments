using FluentValidation;

namespace MedicalAppointments.Application.Auditing;

public sealed class AuditLogQueryValidator : AbstractValidator<AuditLogQuery>
{
    public AuditLogQueryValidator()
    {
        RuleFor(x => x.FromDate).GreaterThan(DateOnly.MinValue);
        RuleFor(x => x.ToDate).GreaterThan(DateOnly.MinValue).LessThan(DateOnly.MaxValue);
        RuleFor(x => x).Must(x => x.ToDate >= x.FromDate)
            .WithMessage("ToDate must be on or after FromDate.");
        RuleFor(x => x).Must(x => x.ToDate.DayNumber - x.FromDate.DayNumber < 366)
            .WithMessage("Audit searches are limited to 366 days.");
        RuleFor(x => x.Page).InclusiveBetween(1, 1_000_000);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Action).MaximumLength(200);
        RuleFor(x => x.EntityName).MaximumLength(200);
    }
}
