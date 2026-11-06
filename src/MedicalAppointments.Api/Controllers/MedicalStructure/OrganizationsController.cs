using Asp.Versioning;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.MedicalStructure.Organizations;
using MedicalAppointments.Contracts.Common;
using MedicalAppointments.Contracts.MedicalStructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointments.Api.Controllers.MedicalStructure;

[ApiController]
[Route("api/v{version:apiVersion}/organizations")]
[ApiVersion("1.0")]
public sealed class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _service;

    public OrganizationsController(IOrganizationService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.OrganizationsRead)]
    public async Task<ActionResult<PaginatedResponse<OrganizationResponse>>> GetList(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new OrganizationQuery(search, isActive, page, pageSize);
        var result = await _service.GetPagedAsync(query, cancellationToken);

        return Ok(new PaginatedResponse<OrganizationResponse>(
            result.Items.Select(ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.OrganizationsRead)]
    public async Task<ActionResult<OrganizationResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.OrganizationsManage)]
    public async Task<ActionResult<OrganizationResponse>> Create(
        [FromBody] CreateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrganizationCommand(
            request.Name,
            request.LegalName,
            request.TaxId,
            request.Email,
            request.Phone,
            request.Address,
            request.City,
            request.Country,
            request.IsActive);

        var result = await _service.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ToResponse(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.OrganizationsManage)]
    public async Task<ActionResult<OrganizationResponse>> Update(
        Guid id,
        [FromBody] UpdateOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateOrganizationCommand(
            id,
            request.Name,
            request.LegalName,
            request.TaxId,
            request.Email,
            request.Phone,
            request.Address,
            request.City,
            request.Country,
            request.IsActive);

        var result = await _service.UpdateAsync(command, cancellationToken);

        return Ok(ToResponse(result));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = Permissions.OrganizationsManage)]
    public async Task<ActionResult<OrganizationResponse>> SetActive(
        Guid id,
        [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetOrganizationActiveCommand(id, request.IsActive);
        var result = await _service.SetActiveAsync(command, cancellationToken);

        return Ok(ToResponse(result));
    }

    private static OrganizationResponse ToResponse(OrganizationDto dto)
    {
        return new OrganizationResponse(
            dto.Id,
            dto.Name,
            dto.LegalName,
            dto.TaxId,
            dto.Email,
            dto.Phone,
            dto.Address,
            dto.City,
            dto.Country,
            dto.IsActive,
            dto.CreatedAt,
            dto.UpdatedAt);
    }
}