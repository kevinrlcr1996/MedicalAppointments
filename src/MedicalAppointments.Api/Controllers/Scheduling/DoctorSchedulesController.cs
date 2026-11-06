using Asp.Versioning;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.Scheduling.DoctorSchedules;
using MedicalAppointments.Contracts.Common;
using MedicalAppointments.Contracts.Scheduling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointments.Api.Controllers.Scheduling;

[ApiController]
[Route("api/v{version:apiVersion}/doctor-schedules")]
[ApiVersion("1.0")]
public sealed class DoctorSchedulesController : ControllerBase
{
    private readonly IDoctorScheduleService _service;

    public DoctorSchedulesController(IDoctorScheduleService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.SchedulesRead)]
    public async Task<ActionResult<PaginatedResponse<ScheduleResponse>>> GetList(
        [FromQuery] Guid? doctorId,
        [FromQuery] Guid? clinicId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new DoctorScheduleQuery(doctorId, clinicId, isActive, page, pageSize);
        var result = await _service.GetPagedAsync(query, cancellationToken);

        return Ok(new PaginatedResponse<ScheduleResponse>(
            result.Items.Select(ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.SchedulesRead)]
    public async Task<ActionResult<ScheduleResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.SchedulesManage)]
    public async Task<ActionResult<ScheduleResponse>> Create(
        [FromBody] CreateScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateDoctorScheduleCommand(
            request.DoctorId,
            request.ClinicId,
            request.Name,
            request.IsDefault,
            request.IsActive,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.SlotDurationMinutes,
            request.BufferMinutes,
            request.Days.Select(x => new ScheduleDayInputDto(x.DayOfWeek, x.StartTime, x.EndTime)).ToList());

        var result = await _service.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ToResponse(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.SchedulesManage)]
    public async Task<ActionResult<ScheduleResponse>> Update(
        Guid id,
        [FromBody] UpdateScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDoctorScheduleCommand(
            id,
            request.Name,
            request.IsDefault,
            request.IsActive,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.SlotDurationMinutes,
            request.BufferMinutes,
            request.Days.Select(x => new ScheduleDayInputDto(x.DayOfWeek, x.StartTime, x.EndTime)).ToList());

        var result = await _service.UpdateAsync(command, cancellationToken);

        return Ok(ToResponse(result));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = Permissions.SchedulesManage)]
    public async Task<ActionResult<ScheduleResponse>> SetActive(
        Guid id,
        [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetDoctorScheduleActiveCommand(id, request.IsActive);
        var result = await _service.SetActiveAsync(command, cancellationToken);

        return Ok(ToResponse(result));
    }

    private static ScheduleResponse ToResponse(ScheduleDto dto)
    {
        return new ScheduleResponse(
            dto.Id,
            dto.DoctorId,
            dto.ClinicId,
            dto.Name,
            dto.IsDefault,
            dto.IsActive,
            dto.EffectiveFrom,
            dto.EffectiveTo,
            dto.SlotDurationMinutes,
            dto.BufferMinutes,
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.Days.Select(x => new ScheduleDayResponse(x.DayOfWeek, x.StartTime, x.EndTime)).ToList());
    }
}