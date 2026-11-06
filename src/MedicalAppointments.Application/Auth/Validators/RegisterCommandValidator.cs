using FluentValidation;
using Microsoft.Extensions.Options;

namespace MedicalAppointments.Application.Auth.Validators;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator(IOptions<PasswordOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value;

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(options.MinLength)
            .Must(password => MeetsPolicy(password, options))
            .WithMessage("Password does not meet security policy.");

        RuleFor(x => x.FirstName)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.LastName));
    }

    private static bool MeetsPolicy(string password, PasswordOptions options)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < options.MinLength)
        {
            return false;
        }

        var hasDigit = password.Any(char.IsDigit);
        var hasLetter = password.Any(char.IsLetter);
        var hasSpecial = password.Any(x => !char.IsLetterOrDigit(x));

        return (!options.RequireDigit || hasDigit)
            && (!options.RequireLetter || hasLetter)
            && (!options.RequireSpecialCharacter || hasSpecial);
    }
}