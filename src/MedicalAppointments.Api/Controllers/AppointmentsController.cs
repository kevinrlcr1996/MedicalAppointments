using Asp.Versioning;
using MedicalAppointments.Application.Appointments;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Contracts.Appointments;
using MedicalAppointments.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointments.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/appointments")]
[ApiVersion("1.0")]
public sealed class AppointmentsController : ControllerBase
{
    private readonly IAppointmentQueryService _queryService;
    private readonly IAppointmentBookingService _bookingService;
    private readonly IAppointmentWorkflowService _workflowService;

    public AppointmentsController(
        IAppointmentQueryService queryService,
        IAppointmentBookingService bookingService,
        IAppointmentWorkflowService workflowService)
    {
        _queryService = queryService;
        _bookingService = bookingService;
        _workflowService = workflowService;
    }

    [HttpGet]
    [Authorize(Policy = Policies.AppointmentsRead)]
    public async Task<ActionResult<PaginatedResponse<AppointmentResponse>>> GetList(
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? doctorId,
        [FromQuery] Guid? clinicId,
        [FromQuery] Guid? roomId,
        [FromQuery] Domain.Appointments.AppointmentStatus? status,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new AppointmentQuery(
            patientId,
            doctorId,
            clinicId,
            roomId,
            status,
            fromUtc,
            toUtc,
            page,
            pageSize);

        var result = await _queryService.GetPagedAsync(query, cancellationToken);

        return Ok(new PaginatedResponse<AppointmentResponse>(
            result.Items.Select(ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.AppointmentsRead)]
    public async Task<ActionResult<AppointmentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _queryService.GetByIdAsync(id, cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost]
    [Authorize(Policy = Policies.AppointmentsCreate)]
    public async Task<ActionResult<AppointmentResponse>> Create(
        [FromBody] CreateAppointmentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new CreateAppointmentCommand(
            request.PatientId,
            request.DoctorId,
            request.ClinicId,
            request.RoomId,
            request.LocalDate,
            request.StartAtUtc,
            request.EndAtUtc,
            request.Reason,
            request.Notes);

        var result = await _bookingService.CreateAsync(command, idempotencyKey ?? string.Empty, cancellationToken);

        if (result.Replayed)
        {
            return Ok(ToResponse(result.Value));
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, ToResponse(result.Value));
    }

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = Policies.AppointmentsCreate)]
    public async Task<ActionResult<AppointmentResponse>> Confirm(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _workflowService.ConfirmAsync(id, cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Policies.AppointmentsCancel)]
    public async Task<ActionResult<AppointmentResponse>> Cancel(
        Guid id,
        [FromBody] CancelAppointmentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new CancelAppointmentCommand(id, request.Reason);
        var result = await _bookingService.CancelAsync(command, idempotencyKey ?? string.Empty, cancellationToken);

        return Ok(ToResponse(result.Value));
    }

    [HttpPost("{id:guid}/reschedule")]
    [Authorize(Policy = Policies.AppointmentsReschedule)]
    public async Task<ActionResult<RescheduleAppointmentResponse>> Reschedule(
        Guid id,
        [FromBody] RescheduleAppointmentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var command = new RescheduleAppointmentCommand(
            id,
            request.NewLocalDate,
            request.NewStartAtUtc,
            request.NewEndAtUtc,
            request.Reason);

        var result = await _bookingService.RescheduleAsync(command, idempotencyKey ?? string.Empty, cancellationToken);

        return Ok(new RescheduleAppointmentResponse(
            ToResponse(result.Value.OldAppointment),
            ToResponse(result.Value.NewAppointment)));
    }

    [HttpPost("{id:guid}/check-in")]
    [Authorize(Policy = Policies.AppointmentsCheckIn)]
    public async Task<ActionResult<AppointmentResponse>> CheckIn(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _workflowService.CheckInAsync(id, cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost("{id:guid}/start")]
    [Authorize(Policy = Policies.AppointmentsCheckIn)]
    public async Task<ActionResult<AppointmentResponse>> Start(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _workflowService.StartAsync(id, cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = Policies.AppointmentsComplete)]
    public async Task<ActionResult<AppointmentResponse>> Complete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _workflowService.CompleteAsync(id, cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost("{id:guid}/no-show")]
    [Authorize(Policy = Policies.AppointmentsMarkNoShow)]
    public async Task<ActionResult<AppointmentResponse>> MarkNoShow(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _workflowService.MarkNoShowAsync(id, cancellationToken);
        return Ok(ToResponse(result));
    }

    private static AppointmentResponse ToResponse(AppointmentDto dto)
    {
        return new AppointmentResponse(
            dto.Id,
            dto.PatientId,
            dto.PatientFullName,
            dto.DoctorId,
            dto.DoctorFullName,
            dto.ClinicId,
            dto.ClinicName,
            dto.RoomId,
            dto.RoomName,
            dto.LocalDate,
            dto.StartAtUtc,
            dto.EndAtUtc,
            dto.Status,
            dto.Reason,
            dto.Notes,
            dto.OriginalAppointmentId,
            dto.CancelledAt,
            dto.CancelledByUserId,
            dto.CancellationReason,
            dto.CreatedAt,
            dto.UpdatedAt);
    }
}