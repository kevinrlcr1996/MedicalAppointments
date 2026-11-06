using Asp.Versioning;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.MedicalStructure.Specialties;
using MedicalAppointments.Contracts.Common;
using MedicalAppointments.Contracts.MedicalStructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointments.Api.Controllers.MedicalStructure;

[ApiController]
[Route("api/v{version:apiVersion}/specialties")]
[ApiVersion("1.0")]
public sealed class SpecialtiesController : ControllerBase
{
    private readonly ISpecialtyService _service;

    public SpecialtiesController(ISpecialtyService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.SpecialtiesRead)]
    public async Task<ActionResult<PaginatedResponse<SpecialtyResponse>>> GetList(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new SpecialtyQuery(search, isActive, page, pageSize);
        var result = await _service.GetPagedAsync(query, cancellationToken);

        return Ok(new PaginatedResponse<SpecialtyResponse>(
            result.Items.Select(ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.SpecialtiesRead)]
    public async Task<ActionResult<SpecialtyResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.SpecialtiesManage)]
    public async Task<ActionResult<SpecialtyResponse>> Create(
        [FromBody] CreateSpecialtyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateSpecialtyCommand(
            request.Name,
            request.Code,
            request.Description,
            request.IsActive);

        var result = await _service.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ToResponse(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.SpecialtiesManage)]
    public async Task<ActionResult<SpecialtyResponse>> Update(
        Guid id,
        [FromBody] UpdateSpecialtyRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSpecialtyCommand(
            id,
            request.Name,
            request.Code,
            request.Description,
            request.IsActive);

        var result = await _service.UpdateAsync(command, cancellationToken);

        return Ok(ToResponse(result));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = Permissions.SpecialtiesManage)]
    public async Task<ActionResult<SpecialtyResponse>> SetActive(
        Guid id,
        [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetSpecialtyActiveCommand(id, request.IsActive);
        var result = await _service.SetActiveAsync(command, cancellationToken);

        return Ok(ToResponse(result));
    }

    private static SpecialtyResponse ToResponse(SpecialtyDto dto)
    {
        return new SpecialtyResponse(
            dto.Id,
            dto.Name,
            dto.Code,
            dto.Description,
            dto.IsActive,
            dto.CreatedAt,
            dto.UpdatedAt);
    }
}