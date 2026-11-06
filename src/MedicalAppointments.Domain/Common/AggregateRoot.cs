namespace MedicalAppointments.Domain.Common;

public abstract class AggregateRoot
{
    public Guid Id { get; protected set; }

    public DateTimeOffset CreatedAt { get; protected set; }

    public DateTimeOffset UpdatedAt { get; protected set; }

    public byte[] RowVersion { get; protected set; } = Array.Empty<byte>();

    protected void Touch(DateTimeOffset now)
    {
        UpdatedAt = now;
    }
}