namespace MedicalAppointments.Application.Auth;

public sealed record RequestContext(
    string? IpAddress,
    string? UserAgent);