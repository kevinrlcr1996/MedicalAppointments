namespace MedicalAppointments.Application.Common;

public interface IRequestMetadataProvider
{
    string? IpAddress { get; }
    string? UserAgent { get; }
}