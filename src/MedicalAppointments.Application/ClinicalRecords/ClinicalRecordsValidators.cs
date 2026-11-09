using FluentValidation;
using MedicalAppointments.Application.ClinicalRecords.Common;

namespace MedicalAppointments.Application.ClinicalRecords;

public sealed class CreateClinicalNoteCommandValidator : AbstractValidator<CreateClinicalNoteCommand>
{
    public CreateClinicalNoteCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(20000);
    }
}

public sealed class UpdateClinicalNoteCommandValidator : AbstractValidator<UpdateClinicalNoteCommand>
{
    public UpdateClinicalNoteCommandValidator()
    {
        RuleFor(x => x.NoteId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(20000);
    }
}

public sealed class CorrectClinicalNoteCommandValidator : AbstractValidator<CorrectClinicalNoteCommand>
{
    public CorrectClinicalNoteCommandValidator()
    {
        RuleFor(x => x.NoteId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(20000);
    }
}