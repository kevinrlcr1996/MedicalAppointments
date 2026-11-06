namespace MedicalAppointments.Application.Common;

public interface ICorrelationIdProvider
{
    string? CorrelationId { get; }
}