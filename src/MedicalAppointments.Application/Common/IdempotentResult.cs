namespace MedicalAppointments.Application.Common;

public sealed record IdempotentResult<T>(
    T Value,
    bool Replayed);