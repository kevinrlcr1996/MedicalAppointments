namespace MedicalAppointments.Application.Auth;

public interface IIdentityEmailLinkBuilder
{
    string BuildEmailConfirmationLink(string token);

    string BuildPasswordResetLink(string token);
}