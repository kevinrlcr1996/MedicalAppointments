using FluentValidation;

namespace MedicalAppointments.Application.MedicalStructure.Patients;

public sealed class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.BloodType).IsInEnum();
        RuleFor(x => x.IdentificationNumber).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.IdentificationNumber));
        RuleFor(x => x.Phone).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.Email).EmailAddress().MaximumLength(320).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Address));
        RuleFor(x => x.EmergencyContact).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.EmergencyContact));
        RuleFor(x => x.EmergencyPhone).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.EmergencyPhone));
        RuleFor(x => x.InsuranceProvider).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.InsuranceProvider));
        RuleFor(x => x.InsuranceNumber).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.InsuranceNumber));
    }
}

public sealed class UpdatePatientCommandValidator : AbstractValidator<UpdatePatientCommand>
{
    public UpdatePatientCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.BloodType).IsInEnum();
        RuleFor(x => x.IdentificationNumber).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.IdentificationNumber));
        RuleFor(x => x.Phone).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.Email).EmailAddress().MaximumLength(320).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Address));
        RuleFor(x => x.EmergencyContact).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.EmergencyContact));
        RuleFor(x => x.EmergencyPhone).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.EmergencyPhone));
        RuleFor(x => x.InsuranceProvider).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.InsuranceProvider));
        RuleFor(x => x.InsuranceNumber).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.InsuranceNumber));
    }
}

public sealed class CreateSelfPatientCommandValidator : AbstractValidator<CreateSelfPatientCommand>
{
    public CreateSelfPatientCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.BloodType).IsInEnum();
        RuleFor(x => x.IdentificationNumber).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.IdentificationNumber));
        RuleFor(x => x.Phone).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.Email).EmailAddress().MaximumLength(320).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Address));
        RuleFor(x => x.EmergencyContact).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.EmergencyContact));
        RuleFor(x => x.EmergencyPhone).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.EmergencyPhone));
        RuleFor(x => x.InsuranceProvider).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.InsuranceProvider));
        RuleFor(x => x.InsuranceNumber).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.InsuranceNumber));
    }
}

public sealed class UpdateSelfPatientCommandValidator : AbstractValidator<UpdateSelfPatientCommand>
{
    public UpdateSelfPatientCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.BloodType).IsInEnum();
        RuleFor(x => x.IdentificationNumber).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.IdentificationNumber));
        RuleFor(x => x.Phone).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.Email).EmailAddress().MaximumLength(320).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Address).MaximumLength(500).When(x => !string.IsNullOrWhiteSpace(x.Address));
        RuleFor(x => x.EmergencyContact).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.EmergencyContact));
        RuleFor(x => x.EmergencyPhone).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.EmergencyPhone));
        RuleFor(x => x.InsuranceProvider).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.InsuranceProvider));
        RuleFor(x => x.InsuranceNumber).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.InsuranceNumber));
    }
}