using Asp.Versioning;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.Scheduling.Blockouts;
using MedicalAppointments.Contracts.Common;
using MedicalAppointments.Contracts.Scheduling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointments.Api.Controllers.Scheduling;

[ApiController]
[Route("api/v{version:apiVersion}/blockouts")]
[ApiVersion("1.0")]
public sealed class BlockoutsController : ControllerBase
{
    private readonly IBlockoutService _service;

    public BlockoutsController(IBlockoutService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.SchedulesRead)]
    public async Task<ActionResult<PaginatedResponse<BlockoutResponse>>> GetList(
        [FromQuery] Guid? doctorId,
        [FromQuery] Guid? clinicId,
        [FromQuery] Guid? roomId,
        [FromQuery] Guid? resourceId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new BlockoutQuery(doctorId, clinicId, roomId, resourceId, isActive, page, pageSize);
        var result = await _service.GetPagedAsync(query, cancellationToken);

        return Ok(new PaginatedResponse<BlockoutResponse>(
            result.Items.Select(ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.SchedulesRead)]
    public async Task<ActionResult<BlockoutResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.SchedulesManage)]
    public async Task<ActionResult<BlockoutResponse>> Create(
        [FromBody] CreateBlockoutRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateBlockoutCommand(
            request.DoctorId,
            request.ClinicId,
            request.RoomId,
            request.ResourceId,
            request.StartAt,
            request.EndAt,
            request.Reason,
            request.IsActive);

        var result = await _service.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ToResponse(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.SchedulesManage)]
    public async Task<ActionResult<BlockoutResponse>> Update(
        Guid id,
        [FromBody] UpdateBlockoutRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBlockoutCommand(
            id,
            request.DoctorId,
            request.ClinicId,
            request.RoomId,
            request.ResourceId,
            request.StartAt,
            request.EndAt,
            request.Reason,
            request.IsActive);

        var result = await _service.UpdateAsync(command, cancellationToken);

        return Ok(ToResponse(result));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = Permissions.SchedulesManage)]
    public async Task<ActionResult<BlockoutResponse>> SetActive(
        Guid id,
        [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetBlockoutActiveCommand(id, request.IsActive);
        var result = await _service.SetActiveAsync(command, cancellationToken);

        return Ok(ToResponse(result));
    }

    private static BlockoutResponse ToResponse(BlockoutDto dto)
    {
        return new BlockoutResponse(
            dto.Id,
            dto.DoctorId,
            dto.ClinicId,
            dto.RoomId,
            dto.ResourceId,
            dto.StartAt,
            dto.EndAt,
            dto.Reason,
            dto.IsActive,
            dto.CreatedAt,
            dto.UpdatedAt);
    }
}