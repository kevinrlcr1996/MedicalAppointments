using Asp.Versioning;
using MedicalAppointments.Application.Auth;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MedicalAppointments.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/dev")]
[ApiVersion("1.0")]
[AllowAnonymous]
public sealed class DevAuthController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly IAccessTokenIssuer _tokenIssuer;

    public DevAuthController(
        IWebHostEnvironment environment,
        IAccessTokenIssuer tokenIssuer)
    {
        _environment = environment;
        _tokenIssuer = tokenIssuer;
    }

    [HttpPost("token")]
    [EnableRateLimiting("dev-token")]
    [ProducesResponseType(typeof(DevTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<DevTokenResponse> IssueDevToken([FromBody] DevTokenRequestDto? request)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var userId = request?.UserId ?? DevelopmentPrincipals.SuperAdminId;
        var userName = string.IsNullOrWhiteSpace(request?.UserName)
            ? DevelopmentPrincipals.SuperAdminUserName
            : request.UserName;

        var issued = _tokenIssuer.Issue(new AccessTokenRequest(
            userId,
            userName,
            userName,
            DevelopmentPrincipals.SuperAdminRoles,
            DevelopmentPrincipals.SuperAdminPermissions,
            "development"));

        return Ok(new DevTokenResponse(
            issued.Token,
            "Bearer",
            issued.ExpiresAt));
    }
}