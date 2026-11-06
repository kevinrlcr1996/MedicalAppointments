using Asp.Versioning;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.Scheduling.Holidays;
using MedicalAppointments.Contracts.Common;
using MedicalAppointments.Contracts.Scheduling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointments.Api.Controllers.Scheduling;

[ApiController]
[Route("api/v{version:apiVersion}/holidays")]
[ApiVersion("1.0")]
public sealed class HolidaysController : ControllerBase
{
    private readonly IHolidayService _service;

    public HolidaysController(IHolidayService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.SchedulesRead)]
    public async Task<ActionResult<PaginatedResponse<HolidayResponse>>> GetList(
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? clinicId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new HolidayQuery(organizationId, clinicId, isActive, page, pageSize);
        var result = await _service.GetPagedAsync(query, cancellationToken);

        return Ok(new PaginatedResponse<HolidayResponse>(
            result.Items.Select(ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.SchedulesRead)]
    public async Task<ActionResult<HolidayResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.SchedulesManage)]
    public async Task<ActionResult<HolidayResponse>> Create(
        [FromBody] CreateHolidayRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateHolidayCommand(
            request.OrganizationId,
            request.ClinicId,
            request.Date,
            request.Name,
            request.Description,
            request.IsActive);

        var result = await _service.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ToResponse(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.SchedulesManage)]
    public async Task<ActionResult<HolidayResponse>> Update(
        Guid id,
        [FromBody] UpdateHolidayRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateHolidayCommand(
            id,
            request.OrganizationId,
            request.ClinicId,
            request.Date,
            request.Name,
            request.Description,
            request.IsActive);

        var result = await _service.UpdateAsync(command, cancellationToken);

        return Ok(ToResponse(result));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = Permissions.SchedulesManage)]
    public async Task<ActionResult<HolidayResponse>> SetActive(
        Guid id,
        [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetHolidayActiveCommand(id, request.IsActive);
        var result = await _service.SetActiveAsync(command, cancellationToken);

        return Ok(ToResponse(result));
    }

    private static HolidayResponse ToResponse(HolidayDto dto)
    {
        return new HolidayResponse(
            dto.Id,
            dto.OrganizationId,
            dto.ClinicId,
            dto.Date,
            dto.Name,
            dto.Description,
            dto.IsActive,
            dto.CreatedAt,
            dto.UpdatedAt);
    }
}