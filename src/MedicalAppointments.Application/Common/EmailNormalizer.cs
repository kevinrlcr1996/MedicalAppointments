namespace MedicalAppointments.Application.Common;

public static class EmailNormalizer
{
    public static string Normalize(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return email.Trim().ToUpperInvariant();
    }
}