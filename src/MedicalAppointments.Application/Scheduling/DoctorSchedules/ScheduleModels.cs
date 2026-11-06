namespace MedicalAppointments.Application.Scheduling.DoctorSchedules;

public sealed record ScheduleDayDto(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record ScheduleDto(
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
    IReadOnlyList<ScheduleDayDto> Days);

public sealed record ScheduleDayInputDto(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);

public sealed record CreateDoctorScheduleCommand(
    Guid DoctorId,
    Guid ClinicId,
    string Name,
    bool IsDefault,
    bool IsActive,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    int SlotDurationMinutes,
    int BufferMinutes,
    IReadOnlyList<ScheduleDayInputDto> Days);

public sealed record UpdateDoctorScheduleCommand(
    Guid Id,
    string Name,
    bool IsDefault,
    bool IsActive,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    int SlotDurationMinutes,
    int BufferMinutes,
    IReadOnlyList<ScheduleDayInputDto> Days);

public sealed record SetDoctorScheduleActiveCommand(
    Guid Id,
    bool IsActive);

public sealed record DoctorScheduleQuery(
    Guid? DoctorId,
    Guid? ClinicId,
    bool? IsActive,
    int Page,
    int PageSize);