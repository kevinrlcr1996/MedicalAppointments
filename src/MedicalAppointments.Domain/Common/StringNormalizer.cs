namespace MedicalAppointments.Domain.Common;

public static class StringNormalizer
{
    public static string NormalizeUpper(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToUpperInvariant();
    }

    public static string? NormalizeUpperOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
    }
}