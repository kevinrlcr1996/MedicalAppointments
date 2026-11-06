using Asp.Versioning;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.Scheduling.ScheduleExceptions;
using MedicalAppointments.Contracts.Common;
using MedicalAppointments.Contracts.Scheduling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointments.Api.Controllers.Scheduling;

[ApiController]
[Route("api/v{version:apiVersion}/schedule-exceptions")]
[ApiVersion("1.0")]
public sealed class ScheduleExceptionsController : ControllerBase
{
    private readonly IScheduleExceptionService _service;

    public ScheduleExceptionsController(IScheduleExceptionService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.SchedulesRead)]
    public async Task<ActionResult<PaginatedResponse<ScheduleExceptionResponse>>> GetList(
        [FromQuery] Guid? doctorId,
        [FromQuery] Guid? clinicId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ScheduleExceptionQuery(doctorId, clinicId, isActive, page, pageSize);
        var result = await _service.GetPagedAsync(query, cancellationToken);

        return Ok(new PaginatedResponse<ScheduleExceptionResponse>(
            result.Items.Select(ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.SchedulesRead)]
    public async Task<ActionResult<ScheduleExceptionResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.SchedulesManage)]
    public async Task<ActionResult<ScheduleExceptionResponse>> Create(
        [FromBody] CreateScheduleExceptionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateScheduleExceptionCommand(
            request.DoctorId,
            request.ClinicId,
            request.StartDate,
            request.EndDate,
            request.StartTime,
            request.EndTime,
            request.Type,
            request.Reason,
            request.IsActive);

        var result = await _service.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ToResponse(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.SchedulesManage)]
    public async Task<ActionResult<ScheduleExceptionResponse>> Update(
        Guid id,
        [FromBody] UpdateScheduleExceptionRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateScheduleExceptionCommand(
            id,
            request.StartDate,
            request.EndDate,
            request.StartTime,
            request.EndTime,
            request.Type,
            request.Reason,
            request.IsActive);

        var result = await _service.UpdateAsync(command, cancellationToken);

        return Ok(ToResponse(result));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = Permissions.SchedulesManage)]
    public async Task<ActionResult<ScheduleExceptionResponse>> SetActive(
        Guid id,
        [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetScheduleExceptionActiveCommand(id, request.IsActive);
        var result = await _service.SetActiveAsync(command, cancellationToken);

        return Ok(ToResponse(result));
    }

    private static ScheduleExceptionResponse ToResponse(ScheduleExceptionDto dto)
    {
        return new ScheduleExceptionResponse(
            dto.Id,
            dto.DoctorId,
            dto.ClinicId,
            dto.StartDate,
            dto.EndDate,
            dto.StartTime,
            dto.EndTime,
            dto.Type,
            dto.Reason,
            dto.IsActive,
            dto.CreatedAt,
            dto.UpdatedAt);
    }
}