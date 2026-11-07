using MedicalAppointments.Application.WaitingRoom.Common;

namespace MedicalAppointments.Application.WaitingRoom;

public interface IWaitingRoomCallService
{
    Task<WaitingRoomCardDto> CheckInToRoomAsync(CheckInToRoomCommand command, CancellationToken cancellationToken = default);
    Task<WaitingRoomCardDto> CallNextAsync(CallNextCommand command, CancellationToken cancellationToken = default);
    Task<WaitingRoomCardDto> AnswerAsync(AnswerCallCommand command, CancellationToken cancellationToken = default);
    Task<WaitingRoomCardDto> SkipAsync(SkipCallCommand command, CancellationToken cancellationToken = default);
    Task<WaitingRoomCardDto> RecallAsync(RecallCallCommand command, CancellationToken cancellationToken = default);
    Task<WaitingRoomCardDto> CompleteAsync(CompleteCallCommand command, CancellationToken cancellationToken = default);
}