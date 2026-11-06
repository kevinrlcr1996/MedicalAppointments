namespace MedicalAppointments.Application.Appointments;

public interface IAppointmentWorkflowService
{
    Task<AppointmentDto> ConfirmAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppointmentDto> CheckInAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppointmentDto> StartAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppointmentDto> CompleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppointmentDto> MarkNoShowAsync(Guid id, CancellationToken cancellationToken = default);
}