using System.Security.Cryptography;
using MedicalAppointments.Application.Auth;

namespace MedicalAppointments.Infrastructure.Identity.Security;

public sealed class CryptographicSecureTokenGenerator : ISecureTokenGenerator
{
    public string Generate(int byteLength = 32)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(byteLength, 16);

        var bytes = RandomNumberGenerator.GetBytes(byteLength);

        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}