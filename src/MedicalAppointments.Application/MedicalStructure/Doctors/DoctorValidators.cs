using FluentValidation;

namespace MedicalAppointments.Application.MedicalStructure.Doctors;

public sealed class CreateDoctorCommandValidator : AbstractValidator<CreateDoctorCommand>
{
    public CreateDoctorCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(320).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LicenseCountry).NotEmpty().MaximumLength(2);
        RuleFor(x => x.Biography).MaximumLength(4000).When(x => !string.IsNullOrWhiteSpace(x.Biography));
        RuleFor(x => x.PhotoUrl).MaximumLength(2048).When(x => !string.IsNullOrWhiteSpace(x.PhotoUrl));
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();

        RuleFor(x => x.SpecialtyIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate specialties are not allowed.");

        RuleFor(x => x.Clinics)
            .Must(clinics => clinics.DistinctBy(x => x.ClinicId).Count() == clinics.Count)
            .WithMessage("Duplicate clinics are not allowed.");

        RuleFor(x => x.Clinics)
            .Must(clinics => clinics.Count(x => x.IsPrimary) <= 1)
            .WithMessage("A doctor can have only one primary clinic.");

        RuleFor(x => x.Languages)
            .Must(languages => languages.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count() == languages.Count(x => !string.IsNullOrWhiteSpace(x)))
            .WithMessage("Duplicate languages are not allowed.");
    }
}

public sealed class UpdateDoctorCommandValidator : AbstractValidator<UpdateDoctorCommand>
{
    public UpdateDoctorCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(320).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LicenseCountry).NotEmpty().MaximumLength(2);
        RuleFor(x => x.Biography).MaximumLength(4000).When(x => !string.IsNullOrWhiteSpace(x.Biography));
        RuleFor(x => x.PhotoUrl).MaximumLength(2048).When(x => !string.IsNullOrWhiteSpace(x.PhotoUrl));
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();

        RuleFor(x => x.SpecialtyIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate specialties are not allowed.");

        RuleFor(x => x.Clinics)
            .Must(clinics => clinics.DistinctBy(x => x.ClinicId).Count() == clinics.Count)
            .WithMessage("Duplicate clinics are not allowed.");

        RuleFor(x => x.Clinics)
            .Must(clinics => clinics.Count(x => x.IsPrimary) <= 1)
            .WithMessage("A doctor can have only one primary clinic.");

        RuleFor(x => x.Languages)
            .Must(languages => languages.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Count() == languages.Count(x => !string.IsNullOrWhiteSpace(x)))
            .WithMessage("Duplicate languages are not allowed.");
    }
}