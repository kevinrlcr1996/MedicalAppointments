namespace MedicalAppointments.Application.Auth;

public interface ISecureTokenGenerator
{
    string Generate(int byteLength = 32);
}