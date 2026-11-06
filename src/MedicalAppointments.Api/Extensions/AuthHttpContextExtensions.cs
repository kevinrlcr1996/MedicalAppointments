using MedicalAppointments.Application.Auth;
using Microsoft.AspNetCore.Http;

namespace MedicalAppointments.Api.Extensions;

public static class AuthHttpContextExtensions
{
    public static void SetRefreshTokenCookie(
        this HttpContext context,
        string refreshToken,
        RefreshTokenOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = options.CookieSecure,
            SameSite = SameSiteMode.Lax,
            Path = options.CookiePath,
            Expires = DateTimeOffset.UtcNow + options.AbsoluteLifetime
        };

        context.Response.Cookies.Append(options.CookieName, refreshToken, cookieOptions);
    }

    public static void DeleteRefreshTokenCookie(
        this HttpContext context,
        RefreshTokenOptions options)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = options.CookieSecure,
            SameSite = SameSiteMode.Lax,
            Path = options.CookiePath,
            Expires = DateTimeOffset.UtcNow.AddDays(-1)
        };

        context.Response.Cookies.Delete(options.CookieName, cookieOptions);
    }

    public static string? GetRefreshTokenCookie(
        this HttpContext context,
        RefreshTokenOptions options)
    {
        return context.Request.Cookies[options.CookieName];
    }

    public static RequestContext ToRequestContext(this HttpContext context)
    {
        return new RequestContext(
            context.Connection.RemoteIpAddress?.ToString(),
            context.Request.Headers.UserAgent.ToString());
    }
}