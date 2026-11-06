using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.Domain.Scheduling;

public sealed class Blockout : AggregateRoot
{
    private Blockout()
    {
    }

    public Guid? DoctorId { get; private set; }

    public Guid? ClinicId { get; private set; }

    public Guid? RoomId { get; private set; }

    public Guid? ResourceId { get; private set; }

    public DateTimeOffset StartAt { get; private set; }

    public DateTimeOffset EndAt { get; private set; }

    public string? Reason { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static Blockout Create(
        Guid? doctorId,
        Guid? clinicId,
        Guid? roomId,
        Guid? resourceId,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        string? reason,
        bool isActive,
        DateTimeOffset now)
    {
        if (!doctorId.HasValue && !roomId.HasValue && !resourceId.HasValue)
        {
            throw new DomainException("A blockout must target at least a doctor, room or resource.");
        }

        var blockout = new Blockout
        {
            Id = Guid.CreateVersion7(),
            CreatedAt = now,
            UpdatedAt = now
        };

        blockout.Update(
            doctorId,
            clinicId,
            roomId,
            resourceId,
            startAt,
            endAt,
            reason,
            isActive,
            now);

        return blockout;
    }

    public void Update(
        Guid? doctorId,
        Guid? clinicId,
        Guid? roomId,
        Guid? resourceId,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        string? reason,
        bool isActive,
        DateTimeOffset now)
    {
        if (!doctorId.HasValue && !roomId.HasValue && !resourceId.HasValue)
        {
            throw new DomainException("A blockout must target at least a doctor, room or resource.");
        }

        if (endAt <= startAt)
        {
            throw new DomainException("End time must be later than start time.");
        }

        DoctorId = doctorId;
        ClinicId = clinicId;
        RoomId = roomId;
        ResourceId = resourceId;
        StartAt = startAt;
        EndAt = endAt;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        IsActive = isActive;

        Touch(now);
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        IsActive = isActive;
        Touch(now);
    }
}