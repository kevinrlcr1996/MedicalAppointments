namespace MedicalAppointments.Application.Auth;

public sealed record DevTokenRequest(
    Guid UserId,
    string UserName,
    IEnumerable<string> Roles,
    IEnumerable<string> Permissions);