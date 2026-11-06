using MedicalAppointments.Domain.Scheduling;

namespace MedicalAppointments.Application.Scheduling.ScheduleExceptions;

public sealed record ScheduleExceptionDto(
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

public sealed record CreateScheduleExceptionCommand(
    Guid DoctorId,
    Guid ClinicId,
    DateOnly StartDate,
    DateOnly EndDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    ScheduleExceptionType Type,
    string? Reason,
    bool IsActive);

public sealed record UpdateScheduleExceptionCommand(
    Guid Id,
    DateOnly StartDate,
    DateOnly EndDate,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    ScheduleExceptionType Type,
    string? Reason,
    bool IsActive);

public sealed record SetScheduleExceptionActiveCommand(
    Guid Id,
    bool IsActive);

public sealed record ScheduleExceptionQuery(
    Guid? DoctorId,
    Guid? ClinicId,
    bool? IsActive,
    int Page,
    int PageSize);