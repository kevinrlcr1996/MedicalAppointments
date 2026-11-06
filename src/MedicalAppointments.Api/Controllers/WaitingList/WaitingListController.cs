using Asp.Versioning;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.WaitingList;
using MedicalAppointments.Application.WaitingList.Common;
using MedicalAppointments.Contracts.Common;
using MedicalAppointments.Contracts.WaitingList;
using MedicalAppointments.Domain.WaitingList;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointments.Api.Controllers.WaitingList;

[ApiController]
[Route("api/v{version:apiVersion}/waiting-list")]
[ApiVersion("1.0")]
public sealed class WaitingListController : ControllerBase
{
    private readonly IWaitingListService _service;

    public WaitingListController(IWaitingListService service)
    {
        _service = service;
    }

    [HttpGet("entries")]
    [Authorize(Policy = Policies.WaitingListRead)]
    public async Task<ActionResult<PaginatedResponse<WaitingListEntryResponse>>> GetEntries(
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? doctorId,
        [FromQuery] Guid? clinicId,
        [FromQuery] WaitingListEntryStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new WaitingListEntryQuery(patientId, doctorId, clinicId, status, page, pageSize);
        var result = await _service.GetEntriesAsync(query, cancellationToken);

        return Ok(new PaginatedResponse<WaitingListEntryResponse>(
            result.Items.Select(ToEntryResponse).ToList(),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    [HttpGet("entries/{id:guid}")]
    [Authorize(Policy = Policies.WaitingListRead)]
    public async Task<ActionResult<WaitingListEntryResponse>> GetEntry(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetEntryAsync(id, cancellationToken);
        return Ok(ToEntryResponse(result));
    }

    [HttpPost("entries")]
    [Authorize(Policy = Policies.WaitingListManage)]
    public async Task<ActionResult<WaitingListEntryResponse>> CreateEntry(
        [FromBody] CreateWaitingListEntryRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateWaitingListEntryCommand(
            request.PatientId, request.DoctorId, request.ClinicId,
            request.PreferredFromUtc, request.PreferredToUtc, request.Priority, request.Note);

        var result = await _service.CreateEntryAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetEntry), new { id = result.Id }, ToEntryResponse(result));
    }

    [HttpPut("entries/{id:guid}")]
    [Authorize(Policy = Policies.WaitingListManage)]
    public async Task<ActionResult<WaitingListEntryResponse>> UpdateEntry(
        Guid id, [FromBody] UpdateWaitingListEntryRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateWaitingListEntryCommand(
            id, request.ClinicId, request.PreferredFromUtc, request.PreferredToUtc, request.Priority, request.Note);

        var result = await _service.UpdateEntryAsync(command, cancellationToken);
        return Ok(ToEntryResponse(result));
    }

    [HttpDelete("entries/{id:guid}")]
    [Authorize(Policy = Policies.WaitingListManage)]
    public async Task<ActionResult<WaitingListEntryResponse>> CancelEntry(
        Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.CancelEntryAsync(new CancelWaitingListEntryCommand(id), cancellationToken);
        return Ok(ToEntryResponse(result));
    }

    [HttpGet("offers")]
    [Authorize(Policy = Policies.WaitingListRead)]
    public async Task<ActionResult<PaginatedResponse<WaitingListOfferResponse>>> GetOffers(
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? doctorId,
        [FromQuery] Guid? clinicId,
        [FromQuery] WaitingListOfferStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new WaitingListOfferQuery(patientId, doctorId, clinicId, status, page, pageSize);
        var result = await _service.GetOffersAsync(query, cancellationToken);

        return Ok(new PaginatedResponse<WaitingListOfferResponse>(
            result.Items.Select(ToOfferResponse).ToList(),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    [HttpPost("offers/{id:guid}/respond")]
    [Authorize(Policy = Policies.WaitingListManage)]
    public async Task<ActionResult<WaitingListOfferResponse>> RespondOffer(
        Guid id, [FromBody] RespondOfferRequest request, CancellationToken cancellationToken)
    {
        var command = new RespondOfferCommand(id, request.Accept);
        var result = await _service.RespondOfferAsync(command, cancellationToken);
        return Ok(ToOfferResponse(result));
    }

    private static WaitingListEntryResponse ToEntryResponse(WaitingListEntryDto dto) => new(
        dto.Id, dto.PatientId, dto.PatientFullName, dto.DoctorId, dto.DoctorFullName,
        dto.ClinicId, dto.ClinicName, dto.PreferredFromUtc, dto.PreferredToUtc,
        dto.Priority, dto.Status, dto.Note, dto.CreatedAt, dto.UpdatedAt);

    private static WaitingListOfferResponse ToOfferResponse(WaitingListOfferDto dto) => new(
        dto.Id, dto.EntryId, dto.PatientId, dto.DoctorId, dto.DoctorFullName,
        dto.ClinicId, dto.ClinicName, dto.RoomId, dto.LocalDate, dto.SlotStartUtc, dto.SlotEndUtc,
        dto.Status, dto.ExpiresAt, dto.RespondedAt, dto.CreatedAt, dto.UpdatedAt);
}