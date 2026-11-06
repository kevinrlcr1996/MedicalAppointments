using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.Domain.Scheduling;

public sealed class ScheduleException : AggregateRoot
{
    private ScheduleException()
    {
    }

    public Guid DoctorId { get; private set; }

    public Guid ClinicId { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public TimeOnly? StartTime { get; private set; }

    public TimeOnly? EndTime { get; private set; }

    public ScheduleExceptionType Type { get; private set; }

    public string? Reason { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static ScheduleException Create(
        Guid doctorId,
        Guid clinicId,
        DateOnly startDate,
        DateOnly endDate,
        TimeOnly? startTime,
        TimeOnly? endTime,
        ScheduleExceptionType type,
        string? reason,
        bool isActive,
        DateTimeOffset now)
    {
        if (doctorId == Guid.Empty)
        {
            throw new DomainException("Doctor is required.");
        }

        if (clinicId == Guid.Empty)
        {
            throw new DomainException("Clinic is required.");
        }

        var exception = new ScheduleException
        {
            Id = Guid.CreateVersion7(),
            DoctorId = doctorId,
            ClinicId = clinicId,
            CreatedAt = now,
            UpdatedAt = now
        };

        exception.Update(
            startDate,
            endDate,
            startTime,
            endTime,
            type,
            reason,
            isActive,
            now);

        return exception;
    }

    public void Update(
        DateOnly startDate,
        DateOnly endDate,
        TimeOnly? startTime,
        TimeOnly? endTime,
        ScheduleExceptionType type,
        string? reason,
        bool isActive,
        DateTimeOffset now)
    {
        if (endDate < startDate)
        {
            throw new DomainException("End date cannot be earlier than start date.");
        }

        if (startDate != endDate && (startTime.HasValue || endTime.HasValue))
        {
            throw new DomainException("Multi-day exceptions must be all-day exceptions.");
        }

        if (startTime.HasValue != endTime.HasValue)
        {
            throw new DomainException("Start time and end time must be provided together.");
        }

        if (startTime.HasValue && endTime.HasValue && endTime.Value <= startTime.Value)
        {
            throw new DomainException("End time must be later than start time.");
        }

        StartDate = startDate;
        EndDate = endDate;
        StartTime = startTime;
        EndTime = endTime;
        Type = type;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        IsActive = isActive;

        Touch(now);
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        IsActive = isActive;
        Touch(now);
    }

    public bool IsAllDay => !StartTime.HasValue && !EndTime.HasValue;
}