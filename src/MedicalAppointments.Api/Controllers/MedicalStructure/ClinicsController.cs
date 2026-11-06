using Asp.Versioning;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.MedicalStructure.Clinics;
using MedicalAppointments.Contracts.Common;
using MedicalAppointments.Contracts.MedicalStructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointments.Api.Controllers.MedicalStructure;

[ApiController]
[Route("api/v{version:apiVersion}/clinics")]
[ApiVersion("1.0")]
public sealed class ClinicsController : ControllerBase
{
    private readonly IClinicService _service;

    public ClinicsController(IClinicService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.ClinicsRead)]
    public async Task<ActionResult<PaginatedResponse<ClinicResponse>>> GetList(
        [FromQuery] Guid? organizationId,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ClinicQuery(organizationId, search, isActive, page, pageSize);
        var result = await _service.GetPagedAsync(query, cancellationToken);

        return Ok(new PaginatedResponse<ClinicResponse>(
            result.Items.Select(ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.ClinicsRead)]
    public async Task<ActionResult<ClinicResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.ClinicsManage)]
    public async Task<ActionResult<ClinicResponse>> Create(
        [FromBody] CreateClinicRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateClinicCommand(
            request.OrganizationId,
            request.Name,
            request.Code,
            request.Description,
            request.Address,
            request.City,
            request.Country,
            request.Phone,
            request.Email,
            request.TimeZoneId,
            request.IsActive);

        var result = await _service.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ToResponse(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.ClinicsManage)]
    public async Task<ActionResult<ClinicResponse>> Update(
        Guid id,
        [FromBody] UpdateClinicRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateClinicCommand(
            id,
            request.Name,
            request.Code,
            request.Description,
            request.Address,
            request.City,
            request.Country,
            request.Phone,
            request.Email,
            request.TimeZoneId,
            request.IsActive);

        var result = await _service.UpdateAsync(command, cancellationToken);

        return Ok(ToResponse(result));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = Permissions.ClinicsManage)]
    public async Task<ActionResult<ClinicResponse>> SetActive(
        Guid id,
        [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetClinicActiveCommand(id, request.IsActive);
        var result = await _service.SetActiveAsync(command, cancellationToken);

        return Ok(ToResponse(result));
    }

    private static ClinicResponse ToResponse(ClinicDto dto)
    {
        return new ClinicResponse(
            dto.Id,
            dto.OrganizationId,
            dto.OrganizationName,
            dto.Name,
            dto.Code,
            dto.Description,
            dto.Address,
            dto.City,
            dto.Country,
            dto.Phone,
            dto.Email,
            dto.TimeZoneId,
            dto.IsActive,
            dto.CreatedAt,
            dto.UpdatedAt);
    }
}