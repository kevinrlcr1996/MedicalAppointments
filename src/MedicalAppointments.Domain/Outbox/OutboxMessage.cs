using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.Domain.Outbox;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
    }

    public Guid Id { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public string Type { get; private set; } = null!;

    public string Payload { get; private set; } = null!;

    public OutboxStatus Status { get; private set; }

    public int Attempts { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    public string? Error { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? NextAttemptAt { get; private set; }

    public string? LockOwner { get; private set; }

    public DateTimeOffset? LockedAt { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public static OutboxMessage Create(
        string type,
        string payload,
        DateTimeOffset occurredAt,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            Type = type,
            Payload = payload,
            OccurredAt = occurredAt,
            CreatedAt = createdAt,
            Status = OutboxStatus.Pending,
            Attempts = 0,
            NextAttemptAt = occurredAt
        };
    }

    public void MarkProcessing(string owner, DateTimeOffset now)
    {
        if (Status != OutboxStatus.Pending)
        {
            throw new DomainException($"Cannot mark outbox message {Id} as processing from status {Status}.");
        }

        Status = OutboxStatus.Processing;
        LockOwner = owner;
        LockedAt = now;
        NextAttemptAt = null;
    }

    public void MarkProcessed(DateTimeOffset now)
    {
        if (Status != OutboxStatus.Processing)
        {
            throw new DomainException($"Cannot mark outbox message {Id} as processed from status {Status}.");
        }

        Status = OutboxStatus.Processed;
        ProcessedAt = now;
        LockOwner = null;
        LockedAt = null;
        Error = null;
        NextAttemptAt = null;
    }

    public void ScheduleRetry(
        DateTimeOffset now,
        string error,
        int maxAttempts,
        TimeSpan minimumBackoff,
        TimeSpan maximumBackoff)
    {
        if (Status != OutboxStatus.Processing)
        {
            throw new DomainException($"Cannot retry outbox message {Id} from status {Status}.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAttempts, 1);

        Attempts++;
        Error = error;
        LockOwner = null;
        LockedAt = null;

        if (Attempts >= maxAttempts)
        {
            Status = OutboxStatus.Failed;
            NextAttemptAt = null;
            return;
        }

        Status = OutboxStatus.Pending;

        var exponent = Math.Min(Attempts, 10);
        var factor = Math.Pow(2, exponent);
        var backoffTicks = (long)Math.Min(
            maximumBackoff.Ticks,
            minimumBackoff.Ticks * factor);

        NextAttemptAt = now + TimeSpan.FromTicks(backoffTicks);
    }
}