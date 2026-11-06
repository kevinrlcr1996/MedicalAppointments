namespace MedicalAppointments.Contracts.Scheduling;

public sealed record ScheduleDayResponse(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record ScheduleResponse(
    Guid Id,
    Guid DoctorId,
    Guid ClinicId,
    string Name,
    bool IsDefault,
    bool IsActive,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    int SlotDurationMinutes,
    int BufferMinutes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<ScheduleDayResponse> Days);

public sealed record ScheduleDayRequest(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record CreateScheduleRequest(
    Guid DoctorId,
    Guid ClinicId,
    string Name,
    bool IsDefault,
    bool IsActive,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    int SlotDurationMinutes,
    int BufferMinutes,
    IReadOnlyList<ScheduleDayRequest> Days);

public sealed record UpdateScheduleRequest(
    string Name,
    bool IsDefault,
    bool IsActive,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    int SlotDurationMinutes,
    int BufferMinutes,
    IReadOnlyList<ScheduleDayRequest> Days);