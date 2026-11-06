using MedicalAppointments.Application.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Application.MedicalStructure.Rooms;

public interface IRoomStore
{
    Task<bool> ExistsNormalizedNameInClinicAsync(
        Guid clinicId,
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsNormalizedCodeInClinicAsync(
        Guid clinicId,
        string normalizedCode,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Room room, CancellationToken cancellationToken = default);

    Task<Room?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Room>> GetPagedAsync(
        RoomQuery query,
        CancellationToken cancellationToken = default);
}