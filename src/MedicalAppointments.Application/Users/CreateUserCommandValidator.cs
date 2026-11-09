using FluentValidation;
using MedicalAppointments.Application.Auth;
using Microsoft.Extensions.Options;

namespace MedicalAppointments.Application.Users;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator(IOptions<PasswordOptions> optionsAccessor)
    {
        var options = optionsAccessor.Value;

        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(options.MinLength);
        RuleFor(x => x.Roles).NotNull();
    }
}