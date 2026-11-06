using MedicalAppointments.Application.Auth;
using MedicalAppointments.Application.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using ApplicationJwtOptions = MedicalAppointments.Application.Auth.JwtOptions;

namespace MedicalAppointments.Infrastructure.Auth;

public sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly ApplicationJwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public JwtAccessTokenIssuer(
        IOptions<ApplicationJwtOptions> optionsAccessor,
        TimeProvider timeProvider)
    {
        _options = optionsAccessor.Value;
        _timeProvider = timeProvider;
    }

    public IssuedAccessToken Issue(AccessTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _timeProvider.GetUtcNow();
        var expiresAt = now + _options.AccessTokenLifetime;

        var identity = new ClaimsIdentity();

        identity.AddClaim(new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, request.UserId.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, request.UserId.ToString()));
        identity.AddClaim(new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, request.Email));
        identity.AddClaim(new Claim(ClaimTypes.Email, request.Email));

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
        {
            identity.AddClaim(new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Name, request.DisplayName));
            identity.AddClaim(new Claim(ClaimTypes.Name, request.DisplayName));
        }

        identity.AddClaim(new Claim(AppClaimTypes.SecurityStamp, request.SecurityStamp));

        foreach (var role in request.Roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in request.Permissions)
        {
            identity.AddClaim(new Claim(AppClaimTypes.Permission, permission));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = identity,
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);

        return new IssuedAccessToken(token, expiresAt);
    }
}