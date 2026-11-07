using MedicalAppointments.Application.WaitingRoom.Common;

namespace MedicalAppointments.Application.WaitingRoom;

public interface IWaitingRoomBoardService
{
    Task<WaitingRoomBoardDto> GetBoardAsync(
        WaitingRoomBoardQuery query,
        CancellationToken cancellationToken = default);
}