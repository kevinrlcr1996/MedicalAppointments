using MedicalAppointments.Application.Common;
using MedicalAppointments.Domain.Scheduling;

namespace MedicalAppointments.Application.Scheduling.Blockouts;

public interface IBlockoutStore
{
    Task AddAsync(Blockout blockout, CancellationToken cancellationToken = default);

    Task<Blockout?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Blockout>> GetDoctorOverlappingAsync(
        Guid doctorId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Blockout>> GetRoomOverlappingAsync(
        Guid roomId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Blockout>> GetPagedAsync(
        BlockoutQuery query,
        CancellationToken cancellationToken = default);
}
