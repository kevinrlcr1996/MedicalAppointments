using FluentValidation;
using MedicalAppointments.Application.WaitingList.Common;

namespace MedicalAppointments.Application.WaitingList;

public sealed class CreateWaitingListEntryCommandValidator : AbstractValidator<CreateWaitingListEntryCommand>
{
    public CreateWaitingListEntryCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.PreferredFromUtc).NotEmpty();
        RuleFor(x => x.PreferredToUtc).GreaterThan(x => x.PreferredFromUtc);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}

public sealed class UpdateWaitingListEntryCommandValidator : AbstractValidator<UpdateWaitingListEntryCommand>
{
    public UpdateWaitingListEntryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.PreferredFromUtc).NotEmpty();
        RuleFor(x => x.PreferredToUtc).GreaterThan(x => x.PreferredFromUtc);
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}

public sealed class RespondOfferCommandValidator : AbstractValidator<RespondOfferCommand>
{
    public RespondOfferCommandValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();
    }
}