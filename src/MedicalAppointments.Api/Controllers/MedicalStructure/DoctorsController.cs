using Asp.Versioning;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Contracts.Common;
using MedicalAppointments.Contracts.MedicalStructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointments.Api.Controllers.MedicalStructure;

[ApiController]
[Route("api/v{version:apiVersion}/doctors")]
[ApiVersion("1.0")]
public sealed class DoctorsController : ControllerBase
{
    private readonly IDoctorService _service;

    public DoctorsController(IDoctorService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.DoctorsRead)]
    public async Task<ActionResult<PaginatedResponse<DoctorResponse>>> GetList(
        [FromQuery] string? search,
        [FromQuery] Guid? specialtyId,
        [FromQuery] Guid? clinicId,
        [FromQuery] Domain.MedicalStructure.DoctorStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new DoctorQuery(search, specialtyId, clinicId, status, page, pageSize);
        var result = await _service.GetPagedAsync(query, cancellationToken);

        return Ok(new PaginatedResponse<DoctorResponse>(
            result.Items.Select(ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.DoctorsRead)]
    public async Task<ActionResult<DoctorResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.DoctorsManage)]
    public async Task<ActionResult<DoctorResponse>> Create(
        [FromBody] CreateDoctorRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateDoctorCommand(
            request.UserId,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.LicenseNumber,
            request.LicenseCountry,
            request.Biography,
            request.PhotoUrl,
            request.Gender,
            request.DateOfBirth,
            request.Status,
            request.SpecialtyIds,
            request.Clinics.Select(x => new DoctorClinicAssignmentDto(
                x.ClinicId,
                x.IsPrimary,
                x.StartDate,
                x.EndDate)).ToList(),
            request.Languages);

        var result = await _service.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ToResponse(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.DoctorsManage)]
    public async Task<ActionResult<DoctorResponse>> Update(
        Guid id,
        [FromBody] UpdateDoctorRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDoctorCommand(
            id,
            request.UserId,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.LicenseNumber,
            request.LicenseCountry,
            request.Biography,
            request.PhotoUrl,
            request.Gender,
            request.DateOfBirth,
            request.Status,
            request.SpecialtyIds,
            request.Clinics.Select(x => new DoctorClinicAssignmentDto(
                x.ClinicId,
                x.IsPrimary,
                x.StartDate,
                x.EndDate)).ToList(),
            request.Languages);

        var result = await _service.UpdateAsync(command, cancellationToken);

        return Ok(ToResponse(result));
    }

    private static DoctorResponse ToResponse(DoctorDto dto)
    {
        return new DoctorResponse(
            dto.Id,
            dto.UserId,
            dto.FirstName,
            dto.LastName,
            dto.Email,
            dto.Phone,
            dto.LicenseNumber,
            dto.LicenseCountry,
            dto.Biography,
            dto.PhotoUrl,
            dto.Gender,
            dto.DateOfBirth,
            dto.Status,
            dto.CreatedAt,
            dto.UpdatedAt,
            dto.Specialties.Select(x => new DoctorSpecialtyResponse(
                x.SpecialtyId,
                x.Name,
                x.Code)).ToList(),
            dto.Clinics.Select(x => new DoctorClinicResponse(
                x.ClinicId,
                x.ClinicName,
                x.ClinicCode,
                x.IsPrimary,
                x.StartDate,
                x.EndDate)).ToList(),
            dto.Languages);
    }
}