using FluentValidation;
using MedicalAppointments.Application.WaitingRoom.Common;

namespace MedicalAppointments.Application.WaitingRoom;

public sealed class CheckInToRoomCommandValidator : AbstractValidator<CheckInToRoomCommand>
{
    public CheckInToRoomCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Note));
    }
}

public sealed class CallNextCommandValidator : AbstractValidator<CallNextCommand>
{
    public CallNextCommandValidator()
    {
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.LocalDate).NotEmpty();
    }
}

public sealed class SkipCallCommandValidator : AbstractValidator<SkipCallCommand>
{
    public SkipCallCommandValidator()
    {
        RuleFor(x => x.CallId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Reason));
    }
}

public sealed class RecallCallCommandValidator : AbstractValidator<RecallCallCommand>
{
    public RecallCallCommandValidator() => RuleFor(x => x.CallId).NotEmpty();
}

public sealed class AnswerCallCommandValidator : AbstractValidator<AnswerCallCommand>
{
    public AnswerCallCommandValidator() => RuleFor(x => x.CallId).NotEmpty();
}

public sealed class CompleteCallCommandValidator : AbstractValidator<CompleteCallCommand>
{
    public CompleteCallCommandValidator() => RuleFor(x => x.CallId).NotEmpty();
}