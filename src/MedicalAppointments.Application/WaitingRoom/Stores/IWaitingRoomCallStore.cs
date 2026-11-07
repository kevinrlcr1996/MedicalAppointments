using MedicalAppointments.Domain.WaitingRoom;

namespace MedicalAppointments.Application.WaitingRoom.Stores;

public interface IWaitingRoomCallStore
{
    Task AddAsync(WaitingRoomCall call, CancellationToken cancellationToken = default);

    Task<WaitingRoomCall?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);

    Task<int> GetNextSequenceAsync(
        Guid doctorId,
        Guid clinicId,
        DateOnly localDate,
        CancellationToken cancellationToken = default);

    Task<WaitingRoomCall?> GetFirstQueuedAsync(
        Guid doctorId,
        Guid clinicId,
        DateOnly localDate,
        CancellationToken cancellationToken = default);

    // FIX reconciliación: todas las calls en cola (Queued/Skipped) en orden de turno, TRACKED,
    // para que CallNext pueda recorrer, reconciliar huérfanos contra la cita real y persistir.
    Task<IReadOnlyList<WaitingRoomCall>> GetQueuedCallsTrackedOrderedAsync(
        Guid doctorId,
        Guid clinicId,
        DateOnly localDate,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WaitingRoomCall>> GetBoardAsync(
        Guid doctorId,
        Guid clinicId,
        DateOnly localDate,
        CancellationToken cancellationToken = default);
}