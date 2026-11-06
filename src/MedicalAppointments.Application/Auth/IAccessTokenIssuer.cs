namespace MedicalAppointments.Application.Auth;

public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(AccessTokenRequest request);
}

public sealed record AccessTokenRequest(
    Guid UserId,
    string Email,
    string? DisplayName,
    IEnumerable<string> Roles,
    IEnumerable<string> Permissions,
    string SecurityStamp);
