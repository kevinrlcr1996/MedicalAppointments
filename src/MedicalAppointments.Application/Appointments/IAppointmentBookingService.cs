using MedicalAppointments.Application.Common;

namespace MedicalAppointments.Application.Appointments;

public interface IAppointmentBookingService
{
    Task<IdempotentResult<AppointmentDto>> CreateAsync(
        CreateAppointmentCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IdempotentResult<AppointmentDto>> CancelAsync(
        CancelAppointmentCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<IdempotentResult<RescheduleAppointmentResult>> RescheduleAsync(
        RescheduleAppointmentCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}