using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.Domain.Idempotency;

public sealed class IdempotencyRecord
{
    private IdempotencyRecord()
    {
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; } = null!;

    public string Operation { get; private set; } = null!;

    public string RequestHash { get; private set; } = null!;

    public IdempotencyStatus Status { get; private set; }

    public string? ResponsePayload { get; private set; }

    public Guid? UserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public static IdempotencyRecord Create(
        string key,
        string operation,
        string requestHash,
        Guid? userId,
        DateTimeOffset now,
        TimeSpan ttl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestHash);

        return new IdempotencyRecord
        {
            Id = Guid.CreateVersion7(),
            Key = key.Trim(),
            Operation = operation,
            RequestHash = requestHash,
            UserId = userId,
            Status = IdempotencyStatus.InProgress,
            CreatedAt = now,
            ExpiresAt = now + ttl
        };
    }

    public void Complete(string responsePayload, DateTimeOffset now)
    {
        if (Status != IdempotencyStatus.InProgress)
        {
            throw new DomainException($"Cannot complete idempotency record {Id} from status {Status}.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(responsePayload);

        Status = IdempotencyStatus.Completed;
        ResponsePayload = responsePayload;
        CompletedAt = now;
    }

    public void Fail(DateTimeOffset now)
    {
        if (Status != IdempotencyStatus.InProgress)
        {
            return;
        }

        Status = IdempotencyStatus.Failed;
        CompletedAt = now;
    }
}