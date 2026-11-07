using MedicalAppointments.Domain.Appointments;

namespace MedicalAppointments.Application.WaitingRoom.Auth;

public sealed record WaitingRoomAccessSubject(
    Guid? UserId,
    Guid? DoctorId,
    bool CanReadAll,
    bool CanManageAll,
    bool CanOperateAssigned,
    bool CanReadAssigned);

public interface IWaitingRoomAccessService
{
    Task<WaitingRoomAccessSubject> GetSubjectAsync(CancellationToken cancellationToken = default);

    bool CanViewBoard(Guid doctorId, WaitingRoomAccessSubject subject);

    bool CanOperateQueue(Guid doctorId, WaitingRoomAccessSubject subject);

    // Delega en la máquina de citas de Fase 5 según la acción pedida.
    // Toma la ENTIDAD de dominio, igual que IAppointmentAccessService de Fase 5.
    bool CanAnswer(Appointment appointment, WaitingRoomAccessSubject subject);

    bool CanComplete(Appointment appointment, WaitingRoomAccessSubject subject);

    bool CanMarkNoShow(Appointment appointment, WaitingRoomAccessSubject subject);
}