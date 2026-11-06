using Asp.Versioning;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.MedicalStructure.Patients;
using MedicalAppointments.Contracts.Common;
using MedicalAppointments.Contracts.MedicalStructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointments.Api.Controllers.MedicalStructure;

[ApiController]
[Route("api/v{version:apiVersion}/patients")]
[ApiVersion("1.0")]
// Sin policy de clase. Cada endpoint declara la suya.
// /me usa solo [Authorize] (GET) o patients.manage_own (POST/PUT)
// para que el paciente lea/escriba SU ficha sin patients.read global.
public sealed class PatientsController : ControllerBase
{
    private readonly IPatientService _service;
    private readonly ICurrentUserService _currentUser;

    public PatientsController(
        IPatientService service,
        ICurrentUserService currentUser)
    {
        _service = service;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.PatientsRead)]
    public async Task<ActionResult<PaginatedResponse<PatientResponse>>> GetList(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new PatientQuery(search, page, pageSize);
        var result = await _service.GetPagedAsync(query, cancellationToken);

        return Ok(new PaginatedResponse<PatientResponse>(
            result.Items.Select(ToResponse).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<PatientResponse>> GetMe(
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var patient = await _service.GetByUserIdAsync(userId, cancellationToken);

        if (patient is null)
        {
            return NotFound();
        }

        return Ok(ToResponse(patient));
    }

    [HttpPost("me")]
    [Authorize(Policy = Permissions.PatientsManageOwn)]
    public async Task<ActionResult<PatientResponse>> CreateSelf(
        [FromBody] CreateSelfPatientRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var command = new CreateSelfPatientCommand(
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Gender,
            request.IdentificationNumber,
            request.Phone,
            request.Email,
            request.Address,
            request.EmergencyContact,
            request.EmergencyPhone,
            request.BloodType,
            request.InsuranceProvider,
            request.InsuranceNumber);

        var result = await _service.CreateSelfAsync(command, userId, cancellationToken);

        return CreatedAtAction(nameof(GetMe), null, ToResponse(result));
    }

    [HttpPut("me")]
    [Authorize(Policy = Permissions.PatientsManageOwn)]
    public async Task<ActionResult<PatientResponse>> UpdateSelf(
        [FromBody] UpdateSelfPatientRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var command = new UpdateSelfPatientCommand(
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Gender,
            request.IdentificationNumber,
            request.Phone,
            request.Email,
            request.Address,
            request.EmergencyContact,
            request.EmergencyPhone,
            request.BloodType,
            request.InsuranceProvider,
            request.InsuranceNumber);

        var result = await _service.UpdateSelfAsync(command, userId, cancellationToken);

        return Ok(ToResponse(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.PatientsRead)]
    public async Task<ActionResult<PatientResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost]
    [Authorize(Policy = Permissions.PatientsCreate)]
    public async Task<ActionResult<PatientResponse>> Create(
        [FromBody] CreatePatientRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePatientCommand(
            request.UserId,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Gender,
            request.IdentificationNumber,
            request.Phone,
            request.Email,
            request.Address,
            request.EmergencyContact,
            request.EmergencyPhone,
            request.BloodType,
            request.InsuranceProvider,
            request.InsuranceNumber);

        var result = await _service.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ToResponse(result));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.PatientsUpdate)]
    public async Task<ActionResult<PatientResponse>> Update(
        Guid id,
        [FromBody] UpdatePatientRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePatientCommand(
            id,
            request.UserId,
            request.FirstName,
            request.LastName,
            request.DateOfBirth,
            request.Gender,
            request.IdentificationNumber,
            request.Phone,
            request.Email,
            request.Address,
            request.EmergencyContact,
            request.EmergencyPhone,
            request.BloodType,
            request.InsuranceProvider,
            request.InsuranceNumber);

        var result = await _service.UpdateAsync(command, cancellationToken);

        return Ok(ToResponse(result));
    }

    private static PatientResponse ToResponse(PatientDto dto)
    {
        return new PatientResponse(
            dto.Id,
            dto.UserId,
            dto.FirstName,
            dto.LastName,
            dto.DateOfBirth,
            dto.Gender,
            dto.IdentificationNumber,
            dto.Phone,
            dto.Email,
            dto.Address,
            dto.EmergencyContact,
            dto.EmergencyPhone,
            dto.BloodType,
            dto.InsuranceProvider,
            dto.InsuranceNumber,
            dto.CreatedAt,
            dto.UpdatedAt);
    }
}