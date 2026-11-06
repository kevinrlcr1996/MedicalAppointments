using FluentValidation;

namespace MedicalAppointments.Application.Scheduling.ScheduleExceptions;

public sealed class CreateScheduleExceptionCommandValidator : AbstractValidator<CreateScheduleExceptionCommand>
{
    public CreateScheduleExceptionCommandValidator()
    {
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}

public sealed class UpdateScheduleExceptionCommandValidator : AbstractValidator<UpdateScheduleExceptionCommand>
{
    public UpdateScheduleExceptionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}