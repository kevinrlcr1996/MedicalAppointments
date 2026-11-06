using MedicalAppointments.Application.Common;

namespace MedicalAppointments.Application.MedicalStructure.Rooms;

public interface IRoomService
{
    Task<PagedResult<RoomDto>> GetPagedAsync(
        RoomQuery query,
        CancellationToken cancellationToken = default);

    Task<RoomDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<RoomDto> CreateAsync(
        CreateRoomCommand command,
        CancellationToken cancellationToken = default);

    Task<RoomDto> UpdateAsync(
        UpdateRoomCommand command,
        CancellationToken cancellationToken = default);

    Task<RoomDto> SetActiveAsync(
        SetRoomActiveCommand command,
        CancellationToken cancellationToken = default);
}