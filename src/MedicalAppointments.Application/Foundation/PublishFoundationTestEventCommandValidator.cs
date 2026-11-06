using FluentValidation;

namespace MedicalAppointments.Application.Foundation;

public sealed class PublishFoundationTestEventCommandValidator
    : AbstractValidator<PublishFoundationTestEventCommand>
{
    public PublishFoundationTestEventCommandValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("The message is required.")
            .MaximumLength(500)
            .WithMessage("The message must not exceed 500 characters.");
    }
}