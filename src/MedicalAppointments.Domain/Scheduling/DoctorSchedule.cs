using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.Domain.Scheduling;

public sealed class DoctorSchedule : AggregateRoot
{
    private DoctorSchedule()
    {
    }

    public Guid DoctorId { get; private set; }

    public Guid ClinicId { get; private set; }

    public string Name { get; private set; } = null!;

    public bool IsDefault { get; private set; }

    public bool IsActive { get; private set; } = true;

    public DateOnly EffectiveFrom { get; private set; }

    public DateOnly? EffectiveTo { get; private set; }

    public int SlotDurationMinutes { get; private set; } = 30;

    public int BufferMinutes { get; private set; }

    public ICollection<DoctorScheduleDay> Days { get; private set; } = new List<DoctorScheduleDay>();

    public static DoctorSchedule Create(
        Guid doctorId,
        Guid clinicId,
        string name,
        bool isDefault,
        bool isActive,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        int slotDurationMinutes,
        int bufferMinutes,
        IEnumerable<DoctorScheduleDayInput> days,
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

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var schedule = new DoctorSchedule
        {
            Id = Guid.CreateVersion7(),
            DoctorId = doctorId,
            ClinicId = clinicId,
            CreatedAt = now,
            UpdatedAt = now
        };

        schedule.Update(
            name,
            isDefault,
            isActive,
            effectiveFrom,
            effectiveTo,
            slotDurationMinutes,
            bufferMinutes,
            days,
            now);

        return schedule;
    }

    public void Update(
        string name,
        bool isDefault,
        bool isActive,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        int slotDurationMinutes,
        int bufferMinutes,
        IEnumerable<DoctorScheduleDayInput> days,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
        {
            throw new DomainException("Effective to cannot be earlier than effective from.");
        }

        if (slotDurationMinutes < 5 || slotDurationMinutes > 240)
        {
            throw new DomainException("Slot duration must be between 5 and 240 minutes.");
        }

        if (bufferMinutes < 0 || bufferMinutes > 120)
        {
            throw new DomainException("Buffer minutes must be between 0 and 120.");
        }

        Name = name.Trim();
        IsDefault = isDefault;
        IsActive = isActive;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        SlotDurationMinutes = slotDurationMinutes;
        BufferMinutes = bufferMinutes;

        ReplaceDays(days, now);
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        IsActive = isActive;
        Touch(now);
    }

    public void ReplaceDays(IEnumerable<DoctorScheduleDayInput> days, DateTimeOffset now)
    {
        var list = days.ToList();

        if (list.Count == 0)
        {
            throw new DomainException("A schedule must contain at least one day.");
        }

        var duplicated = list
            .GroupBy(x => x.DayOfWeek)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicated.Count > 0)
        {
            throw new DomainException("Duplicate schedule days are not allowed.");
        }

        foreach (var day in list)
        {
            if (day.EndTime <= day.StartTime)
            {
                throw new DomainException("Day end time must be later than start time.");
            }
        }

        var requestedDays = list.ToDictionary(x => x.DayOfWeek);

        foreach (var existingDay in Days.Where(x => !requestedDays.ContainsKey(x.DayOfWeek)).ToList())
        {
            Days.Remove(existingDay);
        }

        foreach (var day in list)
        {
            var existingDay = Days.FirstOrDefault(x => x.DayOfWeek == day.DayOfWeek);

            if (existingDay is null)
            {
                Days.Add(new DoctorScheduleDay(
                    id: Guid.CreateVersion7(),
                    doctorScheduleId: Id,
                    dayOfWeek: day.DayOfWeek,
                    startTime: day.StartTime,
                    endTime: day.EndTime,
                    createdAt: now));
            }
            else
            {
                existingDay.Update(day.StartTime, day.EndTime);
            }
        }

        Touch(now);
    }

    public bool AppliesToDate(DateOnly date)
    {
        if (!IsActive)
        {
            return false;
        }

        if (date < EffectiveFrom)
        {
            return false;
        }

        if (EffectiveTo.HasValue && date > EffectiveTo.Value)
        {
            return false;
        }

        return true;
    }
}

public sealed record DoctorScheduleDayInput(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime);