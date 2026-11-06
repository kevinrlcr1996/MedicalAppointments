using FluentValidation;

namespace MedicalAppointments.Application.Scheduling.Blockouts;

public sealed class CreateBlockoutCommandValidator : AbstractValidator<CreateBlockoutCommand>
{
    public CreateBlockoutCommandValidator()
    {
        RuleFor(x => x.EndAt).GreaterThan(x => x.StartAt);
        RuleFor(x => x.Reason).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Reason));
        RuleFor(x => x)
            .Must(x => x.DoctorId.HasValue || x.RoomId.HasValue || x.ResourceId.HasValue)
            .WithMessage("Blockout must target doctor, room or resource.");
    }
}

public sealed class UpdateBlockoutCommandValidator : AbstractValidator<UpdateBlockoutCommand>
{
    public UpdateBlockoutCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.EndAt).GreaterThan(x => x.StartAt);
        RuleFor(x => x.Reason).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Reason));
        RuleFor(x => x)
            .Must(x => x.DoctorId.HasValue || x.RoomId.HasValue || x.ResourceId.HasValue)
            .WithMessage("Blockout must target doctor, room or resource.");
    }
}