using MedicalAppointments.Domain.Scheduling;

namespace MedicalAppointments.Contracts.Scheduling;

public sealed record ScheduleExceptionResponse(
    Guid Id,
    Guid DoctorId,
    Guid ClinicId,
    DateOnly StartDate,
    DateOnly EndDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    ScheduleExceptionType Type,
    string? Reason,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateScheduleExceptionRequest(
    Guid DoctorId,
    Guid ClinicId,
    DateOnly StartDate,
    DateOnly EndDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    ScheduleExceptionType Type,
    string? Reason,
    bool IsActive);

public sealed record UpdateScheduleExceptionRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    ScheduleExceptionType Type,
    string? Reason,
    bool IsActive);