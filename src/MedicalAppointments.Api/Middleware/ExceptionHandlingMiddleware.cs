using FluentValidation;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Runtime.ExceptionServices;

namespace MedicalAppointments.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response has already started. Cannot write problem details.");
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        var (statusCode, title, detail, errors) = Map(exception);

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Unhandled exception.");
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception: {Title}", title);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        if (context.Items.TryGetValue(CorrelationIdMiddleware.ItemName, out var correlationId) &&
            correlationId is string id)
        {
            problem.Extensions["correlationId"] = id;
        }

        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
    }

    private static (int StatusCode, string Title, string? Detail, object? Errors) Map(Exception exception)
    {
        return exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "One or more validation errors occurred.",
                null,
                validationException.Errors.Select(x => new
                {
                    x.PropertyName,
                    x.ErrorMessage
                }).ToArray()),

            DomainException => (
                StatusCodes.Status400BadRequest,
                "A domain rule was violated.",
                exception.Message,
                null),

            InvalidCredentialsException => (
                StatusCodes.Status401Unauthorized,
                "Authentication failed.",
                exception.Message,
                null),

            TokenInvalidException => (
                StatusCodes.Status401Unauthorized,
                "Invalid token.",
                exception.Message,
                null),

            RefreshTokenReuseDetectedException => (
                StatusCodes.Status401Unauthorized,
                "Security violation detected.",
                exception.Message,
                null),

            EmailNotConfirmedException => (
                StatusCodes.Status403Forbidden,
                "Email not confirmed.",
                exception.Message,
                null),

            AccountLockedException => (
                423,
                "Account locked.",
                exception.Message,
                null),

            ForbiddenException => (
                StatusCodes.Status403Forbidden,
                "Access denied.",
                exception.Message,
                null),

            UnauthorizedAccessException => (
                StatusCodes.Status403Forbidden,
                "Access forbidden.",
                exception.Message,
                null),

            NotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found.",
                exception.Message,
                null),

            ConflictException => (
                StatusCodes.Status409Conflict,
                "Conflict detected.",
                exception.Message,
                null),

            AttachmentScannerUnavailableException => (
                StatusCodes.Status503ServiceUnavailable,
                "Attachment scanner unavailable.",
                exception.Message,
                null),

            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Concurrency conflict.",
                "The record was modified by another user. Please reload and try again.",
                null),

            DbUpdateException dbUpdateException when dbUpdateException.InnerException is SqlException sqlException &&
                (sqlException.Number == 2627 || sqlException.Number == 2601) => (
                StatusCodes.Status409Conflict,
                "Unique constraint violation.",
                "The operation conflicts with an existing record.",
                null),

            PolicyViolationException => (
                StatusCodes.Status409Conflict,
                "Business policy violation.",
                exception.Message,
                null),

            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                null,
                null)
        };
    }
}
