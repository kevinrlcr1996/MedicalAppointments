using Asp.Versioning;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.WaitingRoom;
using MedicalAppointments.Application.WaitingRoom.Common;
using MedicalAppointments.Contracts.WaitingRoom;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointments.Api.Controllers.WaitingRoom;

[ApiController]
[Route("api/v{version:apiVersion}/waiting-room")]
[ApiVersion("1.0")]
public sealed class WaitingRoomController : ControllerBase
{
    private readonly IWaitingRoomBoardService _board;
    private readonly IWaitingRoomCallService _calls;

    public WaitingRoomController(IWaitingRoomBoardService board, IWaitingRoomCallService calls)
    {
        _board = board;
        _calls = calls;
    }

    [HttpGet("board")]
    [Authorize(Policy = Policies.WaitingRoomRead)]
    public async Task<ActionResult<WaitingRoomBoardResponse>> GetBoard(
        [FromQuery] Guid doctorId,
        [FromQuery] Guid clinicId,
        [FromQuery] DateOnly localDate,
        CancellationToken cancellationToken)
    {
        var query = new WaitingRoomBoardQuery(doctorId, clinicId, localDate);
        var result = await _board.GetBoardAsync(query, cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost("check-in")]
    [Authorize(Policy = Policies.WaitingRoomOperate)]
    public async Task<ActionResult<WaitingRoomCardResponse>> CheckInToRoom(
        [FromBody] CheckInToRoomRequest request,
        [FromQuery] Guid appointmentId,
        CancellationToken cancellationToken)
    {
        var command = new CheckInToRoomCommand(appointmentId, request.Note);
        var result = await _calls.CheckInToRoomAsync(command, cancellationToken);
        return Ok(ToCard(result));
    }

    [HttpPost("call-next")]
    [Authorize(Policy = Policies.WaitingRoomOperate)]
    public async Task<ActionResult<WaitingRoomCardResponse>> CallNext(
        [FromBody] CallNextRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CallNextCommand(request.DoctorId, request.ClinicId, request.LocalDate, request.RoomId);
        var result = await _calls.CallNextAsync(command, cancellationToken);
        return Ok(ToCard(result));
    }

    [HttpPost("{callId:guid}/answer")]
    [Authorize(Policy = Policies.WaitingRoomOperate)]
    public async Task<ActionResult<WaitingRoomCardResponse>> Answer(
        Guid callId, CancellationToken cancellationToken)
    {
        var result = await _calls.AnswerAsync(new AnswerCallCommand(callId), cancellationToken);
        return Ok(ToCard(result));
    }

    [HttpPost("{callId:guid}/skip")]
    [Authorize(Policy = Policies.WaitingRoomOperate)]
    public async Task<ActionResult<WaitingRoomCardResponse>> Skip(
        Guid callId, [FromBody] SkipCallRequest request, CancellationToken cancellationToken)
    {
        var result = await _calls.SkipAsync(new SkipCallCommand(callId, request.Reason), cancellationToken);
        return Ok(ToCard(result));
    }

    [HttpPost("{callId:guid}/recall")]
    [Authorize(Policy = Policies.WaitingRoomOperate)]
    public async Task<ActionResult<WaitingRoomCardResponse>> Recall(
        Guid callId, CancellationToken cancellationToken)
    {
        var result = await _calls.RecallAsync(new RecallCallCommand(callId), cancellationToken);
        return Ok(ToCard(result));
    }

    [HttpPost("{callId:guid}/complete")]
    [Authorize(Policy = Policies.WaitingRoomOperate)]
    public async Task<ActionResult<WaitingRoomCardResponse>> Complete(
        Guid callId, CancellationToken cancellationToken)
    {
        var result = await _calls.CompleteAsync(new CompleteCallCommand(callId), cancellationToken);
        return Ok(ToCard(result));
    }

    private static WaitingRoomBoardResponse ToResponse(WaitingRoomBoardDto dto) => new(
        dto.DoctorId, dto.DoctorFullName, dto.ClinicId, dto.ClinicName, dto.LocalDate,
        dto.Queued.Select(ToCard).ToList(),
        dto.Called.Select(ToCard).ToList(),
        dto.InConsultation.Select(ToCard).ToList(),
        dto.Skipped.Select(ToCard).ToList(),
        dto.Completed.Select(ToCard).ToList());

    private static WaitingRoomCardResponse ToCard(WaitingRoomCardDto dto) => new(
        dto.CallId, dto.AppointmentId, dto.SequenceNumber, dto.PatientId, dto.PatientFullName,
        dto.DoctorId, dto.DoctorFullName, dto.ClinicId, dto.ClinicName, dto.RoomId, dto.RoomName,
        dto.LocalDate, dto.Status, dto.RecallCount, dto.CalledAt, dto.AnsweredAt, dto.SkippedAt,
        dto.CompletedAt, dto.AppointmentStartAtUtc, dto.QueuedAt, dto.Note);
}