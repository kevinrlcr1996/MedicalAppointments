using Asp.Versioning;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.ClinicalRecords;
using MedicalAppointments.Application.ClinicalRecords.Common;
using MedicalAppointments.Contracts.ClinicalRecords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointments.Api.Controllers.ClinicalRecords;

[ApiController]
[Route("api/v{version:apiVersion}/clinical-records")]
[ApiVersion("1.0")]
public sealed class ClinicalRecordsController : ControllerBase
{
    private readonly IClinicalRecordsService _service;
    private readonly IClinicalAttachmentService _attachments;

    public ClinicalRecordsController(IClinicalRecordsService service, IClinicalAttachmentService attachments)
    {
        _service = service;
        _attachments = attachments;
    }

    [HttpGet("appointments/{appointmentId:guid}/notes")]
    [Authorize(Policy = Policies.ClinicalRecordsRead)]
    public async Task<ActionResult<IReadOnlyList<ClinicalNoteResponse>>> GetNotes(
        Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await _service.GetNotesByAppointmentAsync(appointmentId, cancellationToken);
        return Ok(result.Select(ToResponse).ToList());
    }

    [HttpPost("appointments/{appointmentId:guid}/notes")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<ClinicalNoteResponse>> CreateNote(
        Guid appointmentId,
        [FromBody] CreateClinicalNoteRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateClinicalNoteCommand(
            appointmentId, request.Category, request.Title, request.Content, request.OccurredAtUtc);

        var result = await _service.CreateNoteAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetNotes), new { appointmentId }, ToResponse(result));
    }

    [HttpPut("notes/{noteId:guid}")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<ClinicalNoteResponse>> UpdateNote(
        Guid noteId,
        [FromBody] UpdateClinicalNoteRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateClinicalNoteCommand(noteId, request.Category, request.Title, request.Content);
        var result = await _service.UpdateNoteAsync(command, cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost("notes/{noteId:guid}/finalize")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<ClinicalNoteResponse>> FinalizeNote(
        Guid noteId, CancellationToken cancellationToken)
    {
        var result = await _service.FinalizeNoteAsync(new FinalizeClinicalNoteCommand(noteId), cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost("notes/{noteId:guid}/corrections")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<ClinicalNoteResponse>> CorrectNote(
        Guid noteId,
        [FromBody] CorrectClinicalNoteRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CorrectClinicalNoteCommand(
            noteId, request.Category, request.Title, request.Content, request.OccurredAtUtc);

        var result = await _service.CorrectNoteAsync(command, cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpGet("notes/{noteId:guid}/attachments")]
    [Authorize(Policy = Policies.ClinicalRecordsRead)]
    public async Task<ActionResult<IReadOnlyList<ClinicalAttachmentResponse>>> ListAttachments(
    Guid noteId, CancellationToken cancellationToken)
    {
        var result = await _attachments.ListByNoteAsync(noteId, cancellationToken);
        return Ok(result.Select(ToAttachmentResponse).ToList());
    }

    [HttpPost("notes/{noteId:guid}/attachments")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    [RequestSizeLimit(20 * 1024 * 1024)] // techo HTTP; el límite de negocio es Attachments:MaxBytes
    public async Task<ActionResult<ClinicalAttachmentResponse>> UploadAttachment(
        Guid noteId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest("No file received.");
        }

        await using var stream = file.OpenReadStream();
        var command = new UploadClinicalAttachmentCommand(
            noteId, file.FileName, file.ContentType, file.Length, stream);

        var result = await _attachments.UploadAsync(command, cancellationToken);
        return Ok(ToAttachmentResponse(result));
    }

    [HttpGet("attachments/{attachmentId:guid}/download")]
    [Authorize(Policy = Policies.ClinicalRecordsRead)]
    public async Task<IActionResult> DownloadAttachment(
        Guid attachmentId, CancellationToken cancellationToken)
    {
        var (content, fileName, contentType) = await _attachments.DownloadAsync(attachmentId, cancellationToken);
        return File(content, contentType, fileName);
    }

    [HttpGet("patients/{patientId:guid}/history")]
    [Authorize(Policy = Policies.ClinicalRecordsRead)]
    public async Task<ActionResult<IReadOnlyList<ClinicalHistoryItemResponse>>> GetPatientHistory(
        Guid patientId, CancellationToken cancellationToken)
    {
        var result = await _service.GetPatientHistoryAsync(patientId, cancellationToken);
        return Ok(result.Select(ToHistoryResponse).ToList());
    }

    private static ClinicalNoteResponse ToResponse(ClinicalNoteDto dto) => new(
        dto.Id, dto.AppointmentId, dto.AuthorUserId, dto.AuthorDoctorId, dto.AuthorDisplayName,
        dto.Category, dto.Title, dto.Content, dto.IsFinal, dto.OccurredAtUtc, dto.FinalizedAt,
        dto.FinalizedByUserId, dto.CorrectsNoteId, dto.CorrectsNoteTitle, dto.CreatedAt, dto.UpdatedAt);

    private static ClinicalAttachmentResponse ToAttachmentResponse(ClinicalAttachmentDto dto) => new(
        dto.Id, dto.NoteId, dto.FileName, dto.ContentType, dto.SizeBytes,
        dto.ScanStatus, dto.ScannedAt, dto.UploadedByUserId, dto.CreatedAt);

    private static ClinicalHistoryItemResponse ToHistoryResponse(ClinicalHistoryItemDto dto) => new(
        dto.NoteId, dto.AppointmentId, dto.AppointmentLocalDate, dto.AppointmentStartAtUtc,
        dto.DoctorFullName, dto.ClinicName, dto.Category, dto.Title, dto.Content, dto.IsFinal,
        dto.AuthorDisplayName, dto.OccurredAtUtc, dto.FinalizedAt, dto.CorrectsNoteId, dto.AttachmentCount);
}