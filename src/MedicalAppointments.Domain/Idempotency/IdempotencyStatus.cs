namespace MedicalAppointments.Domain.Idempotency;

public enum IdempotencyStatus
{
    InProgress = 0,
    Completed = 1,
    Failed = 2
}