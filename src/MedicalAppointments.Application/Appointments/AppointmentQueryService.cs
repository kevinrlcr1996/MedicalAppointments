using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.Appointments.Auth;
using MedicalAppointments.Domain.Appointments;

namespace MedicalAppointments.Application.Appointments;

public sealed class AppointmentQueryService : IAppointmentQueryService
{
    private readonly IAppointmentStore _store;
    private readonly IAppointmentAccessService _accessService;

    public AppointmentQueryService(
        IAppointmentStore store,
        IAppointmentAccessService accessService)
    {
        _store = store;
        _accessService = accessService;
    }

    public async Task<PagedResult<AppointmentDto>> GetPagedAsync(
        AppointmentQuery query,
        CancellationToken cancellationToken = default)
    {
        var subject = await _accessService.GetSubjectAsync(cancellationToken);
        var filter = _accessService.BuildReadFilter(subject);

        // 403 SOLO si no tiene ningún permiso de lectura.
        // Paciente sin ficha => Denied=false, scope vacío => el store devuelve [].
        if (filter.Denied)
        {
            throw new ForbiddenException("You do not have access to appointments.");
        }

        var result = await _store.GetPagedAsync(query, filter, cancellationToken);

        return new PagedResult<AppointmentDto>(
            result.Items.Select(Map).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
    }

    public async Task<AppointmentDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var subject = await _accessService.GetSubjectAsync(cancellationToken);
        var appointment = await _store.GetWithRelationsAsync(id, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        if (!_accessService.CanRead(appointment, subject))
        {
            throw new ForbiddenException("You do not have access to this appointment.");
        }

        return Map(appointment);
    }

    internal static AppointmentDto Map(Appointment appointment)
    {
        return new AppointmentDto(
            appointment.Id,
            appointment.PatientId,
            appointment.Patient is null
                ? string.Empty
                : $"{appointment.Patient.FirstName} {appointment.Patient.LastName}",
            appointment.DoctorId,
            appointment.Doctor is null
                ? string.Empty
                : $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}",
            appointment.ClinicId,
            appointment.Clinic?.Name ?? string.Empty,
            appointment.RoomId,
            appointment.Room?.Name,
            appointment.LocalDate,
            appointment.StartAt,
            appointment.EndAt,
            appointment.Status,
            appointment.Reason,
            appointment.Notes,
            appointment.OriginalAppointmentId,
            appointment.CancelledAt,
            appointment.CancelledByUserId,
            appointment.CancellationReason,
            appointment.CreatedAt,
            appointment.UpdatedAt);
    }
}