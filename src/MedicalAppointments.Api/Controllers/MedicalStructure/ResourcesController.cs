using Asp.Versioning;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.MedicalStructure.Resources;
using MedicalAppointments.Contracts.Common;
using MedicalAppointments.Contracts.MedicalStructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointments.Api.Controllers.MedicalStructure;

[ApiController]
[Route("api/v{version:apiVersion}/resources")]
[ApiVersion("1.0")]
public sealed class ResourcesController : ControllerBase
{
    private readonly IResourceService _service;

    public ResourcesController(IResourceService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.ResourcesRead)]
    public async Task<ActionResult<PaginatedResponse<ResourceResponse>>> GetList(
        [FromQuery] Guid? clinicId,
        [FromQuery] Domain.MedicalStructure.ResourceKind? kind,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new ResourceQuery(clinicId, kind, search, isActive, page, pageSize);
        var result = await _service.GetPagedAsync(query, cancellationToken);

        return Ok(new PaginatedResponse<ResourceResponse>(
            result.Items.Select(ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.ResourcesRead)]
    public async Task<ActionResult<ResourceResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.ResourcesManage)]
    public async Task<ActionResult<ResourceResponse>> Create(
        [FromBody] CreateResourceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateResourceCommand(
            request.ClinicId,
            request.Name,
            request.Code,
            request.Kind,
            request.Description,
            request.IsActive);

        var result = await _service.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ToResponse(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.ResourcesManage)]
    public async Task<ActionResult<ResourceResponse>> Update(
        Guid id,
        [FromBody] UpdateResourceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateResourceCommand(
            id,
            request.Name,
            request.Code,
            request.Kind,
            request.Description,
            request.IsActive);

        var result = await _service.UpdateAsync(command, cancellationToken);

        return Ok(ToResponse(result));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = Permissions.ResourcesManage)]
    public async Task<ActionResult<ResourceResponse>> SetActive(
        Guid id,
        [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetResourceActiveCommand(id, request.IsActive);
        var result = await _service.SetActiveAsync(command, cancellationToken);

        return Ok(ToResponse(result));
    }

    private static ResourceResponse ToResponse(ResourceDto dto)
    {
        return new ResourceResponse(
            dto.Id,
            dto.ClinicId,
            dto.ClinicName,
            dto.Name,
            dto.Code,
            dto.Kind,
            dto.Description,
            dto.IsActive,
            dto.CreatedAt,
            dto.UpdatedAt);
    }
}