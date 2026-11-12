using Asp.Versioning;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.ClinicalRecords;
using MedicalAppointments.Application.ClinicalRecords.Common;
using MedicalAppointments.Contracts.ClinicalRecords;
using MedicalAppointments.Domain.ClinicalRecords;
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
    private readonly IVitalSignsService _vitalSigns;
    private readonly IDiagnosisService _diagnoses;
    private readonly IPatientAllergyService _allergies;
    private readonly IPatientMedicationService _medications;
    private readonly IPrescriptionService _prescriptions;
    private readonly IClinicalDocumentService _documents;

    public ClinicalRecordsController(IClinicalRecordsService service, IClinicalAttachmentService attachments, 
        IVitalSignsService vitalSigns, IDiagnosisService diagnoses, IPatientAllergyService allergies,
        IPatientMedicationService medications, IPrescriptionService prescriptionService,
        IClinicalDocumentService documentService)
    {
        _service = service;
        _attachments = attachments;
        _vitalSigns = vitalSigns;
        _diagnoses = diagnoses;
        _allergies = allergies;
        _medications = medications;
        _prescriptions = prescriptionService;
        _documents = documentService;
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

    [HttpDelete("attachments/{attachmentId:guid}")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<IActionResult> DeleteAttachment(
    Guid attachmentId, CancellationToken cancellationToken)
    {
        await _attachments.DeleteAsync(attachmentId, cancellationToken);
        return NoContent();
    }

    [HttpGet("appointments/{appointmentId:guid}/vital-signs")]
    [Authorize(Policy = Policies.ClinicalRecordsRead)]
    public async Task<ActionResult<IReadOnlyList<VitalSignsResponse>>> ListVitalSigns(Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await _vitalSigns.ListByAppointmentAsync(appointmentId, cancellationToken);
        return Ok(result.Select(ToVitalSignsResponse).ToList());
    }

    [HttpPost("appointments/{appointmentId:guid}/vital-signs")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<VitalSignsResponse>> CreateVitalSigns(Guid appointmentId, [FromBody] CreateVitalSignsRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateVitalSignsCommand(appointmentId, ToMeasurements(request), request.RecordedAtUtc);
        var result = await _vitalSigns.CreateAsync(command, cancellationToken);
        return Ok(ToVitalSignsResponse(result));
    }

    [HttpPut("vital-signs/{id:guid}")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<VitalSignsResponse>> UpdateVitalSigns(Guid id, [FromBody] UpdateVitalSignsRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateVitalSignsCommand(id, ToMeasurements(request));
        var result = await _vitalSigns.UpdateAsync(command, cancellationToken);
        return Ok(ToVitalSignsResponse(result));
    }

    [HttpPost("vital-signs/{id:guid}/finalize")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<VitalSignsResponse>> FinalizeVitalSigns(Guid id, CancellationToken cancellationToken)
    {
        var result = await _vitalSigns.FinalizeAsync(id, cancellationToken);
        return Ok(ToVitalSignsResponse(result));
    }

    [HttpPost("vital-signs/{id:guid}/corrections")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<VitalSignsResponse>> CorrectVitalSigns(Guid id, [FromBody] CorrectVitalSignsRequest request, CancellationToken cancellationToken)
    {
        var command = new CorrectVitalSignsCommand(id, ToMeasurements(request), request.RecordedAtUtc);
        var result = await _vitalSigns.CorrectAsync(command, cancellationToken);
        return Ok(ToVitalSignsResponse(result));
    }

    [HttpGet("appointments/{appointmentId:guid}/diagnoses")]
    [Authorize(Policy = Policies.ClinicalRecordsRead)]
    public async Task<ActionResult<IReadOnlyList<DiagnosisResponse>>> ListDiagnoses(Guid appointmentId, CancellationToken cancellationToken)
    {
        var result = await _diagnoses.ListByAppointmentAsync(appointmentId, cancellationToken);
        return Ok(result.Select(ToDiagnosisResponse).ToList());
    }

    [HttpPost("appointments/{appointmentId:guid}/diagnoses")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<DiagnosisResponse>> CreateDiagnosis(Guid appointmentId, [FromBody] CreateDiagnosisRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateDiagnosisCommand(appointmentId, request.Code, request.DisplayTerm, request.Category, request.OnsetDate, request.Note, request.RecordedAtUtc);
        var result = await _diagnoses.CreateAsync(command, cancellationToken);
        return Ok(ToDiagnosisResponse(result));
    }

    [HttpPut("diagnoses/{id:guid}")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<DiagnosisResponse>> UpdateDiagnosis(Guid id, [FromBody] UpdateDiagnosisRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateDiagnosisCommand(id, request.Code, request.DisplayTerm, request.Category, request.OnsetDate, request.Note);
        var result = await _diagnoses.UpdateAsync(command, cancellationToken);
        return Ok(ToDiagnosisResponse(result));
    }

    [HttpPost("diagnoses/{id:guid}/finalize")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<DiagnosisResponse>> FinalizeDiagnosis(Guid id, CancellationToken cancellationToken)
    {
        var result = await _diagnoses.FinalizeAsync(id, cancellationToken);
        return Ok(ToDiagnosisResponse(result));
    }

    [HttpPost("diagnoses/{id:guid}/corrections")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<DiagnosisResponse>> CorrectDiagnosis(Guid id, [FromBody] CorrectDiagnosisRequest request, CancellationToken cancellationToken)
    {
        var command = new CorrectDiagnosisCommand(id, request.Code, request.DisplayTerm, request.Category, request.OnsetDate, request.Note, request.RecordedAtUtc);
        var result = await _diagnoses.CorrectAsync(command, cancellationToken);
        return Ok(ToDiagnosisResponse(result));
    }

    [HttpGet("patients/{patientId:guid}/allergies")]
    [Authorize(Policy = Policies.ClinicalRecordsRead)]
    public async Task<ActionResult<IReadOnlyList<PatientAllergyResponse>>> ListAllergies(Guid patientId, CancellationToken cancellationToken)
    {
        var result = await _allergies.ListByPatientAsync(patientId, cancellationToken);
        return Ok(result.Select(ToAllergyResponse).ToList());
    }

    [HttpPost("patients/{patientId:guid}/allergies")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<PatientAllergyResponse>> CreateAllergy(Guid patientId, [FromBody] CreateAllergyRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateAllergyCommand(patientId, request.Allergen, request.AllergenClass, request.Severity, request.Reaction, request.OnsetDate, request.RecordedAtUtc);
        var result = await _allergies.CreateAsync(command, cancellationToken);
        return Ok(ToAllergyResponse(result));
    }

    [HttpPut("allergies/{id:guid}")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<PatientAllergyResponse>> UpdateAllergy(Guid id, [FromBody] UpdateAllergyRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateAllergyCommand(id, request.Allergen, request.AllergenClass, request.Severity, request.Reaction, request.OnsetDate);
        var result = await _allergies.UpdateAsync(command, cancellationToken);
        return Ok(ToAllergyResponse(result));
    }

    [HttpPost("allergies/{id:guid}/finalize")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<PatientAllergyResponse>> FinalizeAllergy(Guid id, CancellationToken cancellationToken)
        => Ok(ToAllergyResponse(await _allergies.FinalizeAsync(id, cancellationToken)));

    [HttpPost("allergies/{id:guid}/resolve")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<PatientAllergyResponse>> ResolveAllergy(Guid id, CancellationToken cancellationToken)
        => Ok(ToAllergyResponse(await _allergies.ResolveAsync(id, cancellationToken)));

    [HttpPost("allergies/{id:guid}/unresolve")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<PatientAllergyResponse>> UnresolveAllergy(Guid id, CancellationToken cancellationToken)
        => Ok(ToAllergyResponse(await _allergies.UnresolveAsync(id, cancellationToken)));

    [HttpPost("allergies/{id:guid}/corrections")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<PatientAllergyResponse>> CorrectAllergy(Guid id, [FromBody] CorrectAllergyRequest request, CancellationToken cancellationToken)
    {
        var command = new CorrectAllergyCommand(id, request.PatientId, request.Allergen, request.AllergenClass, request.Severity, request.Reaction, request.OnsetDate, request.RecordedAtUtc);
        var result = await _allergies.CorrectAsync(command, cancellationToken);
        return Ok(ToAllergyResponse(result));
    }

    [HttpGet("patients/{patientId:guid}/medications")]
    [Authorize(Policy = Policies.ClinicalRecordsRead)]
    public async Task<ActionResult<IReadOnlyList<PatientMedicationResponse>>> ListMedications(Guid patientId, CancellationToken cancellationToken)
        => Ok((await _medications.ListByPatientAsync(patientId, cancellationToken)).Select(ToMedicationResponse).ToList());

    [HttpPost("patients/{patientId:guid}/medications")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<PatientMedicationResponse>> CreateMedication(Guid patientId, [FromBody] CreateMedicationRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateMedicationCommand(patientId, request.MedicationName, request.Strength, request.DosageForm, request.Frequency, request.Route, request.StartDate, request.Indication, request.RecordedAtUtc);
        return Ok(ToMedicationResponse(await _medications.CreateAsync(command, cancellationToken)));
    }

    [HttpPut("medications/{id:guid}")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<PatientMedicationResponse>> UpdateMedication(Guid id, [FromBody] UpdateMedicationRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateMedicationCommand(id, request.MedicationName, request.Strength, request.DosageForm, request.Frequency, request.Route, request.StartDate, request.Indication);
        return Ok(ToMedicationResponse(await _medications.UpdateAsync(command, cancellationToken)));
    }

    [HttpPost("medications/{id:guid}/finalize")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<PatientMedicationResponse>> FinalizeMedication(Guid id, CancellationToken cancellationToken)
        => Ok(ToMedicationResponse(await _medications.FinalizeAsync(id, cancellationToken)));

    [HttpPost("medications/{id:guid}/suspend")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<PatientMedicationResponse>> SuspendMedication(Guid id, CancellationToken cancellationToken)
        => Ok(ToMedicationResponse(await _medications.SuspendAsync(id, cancellationToken)));

    [HttpPost("medications/{id:guid}/resume")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<PatientMedicationResponse>> ResumeMedication(Guid id, CancellationToken cancellationToken)
        => Ok(ToMedicationResponse(await _medications.ResumeAsync(id, cancellationToken)));

    [HttpPost("medications/{id:guid}/discontinue")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<PatientMedicationResponse>> DiscontinueMedication(Guid id, CancellationToken cancellationToken)
        => Ok(ToMedicationResponse(await _medications.DiscontinueAsync(id, cancellationToken)));

    [HttpPost("medications/{id:guid}/corrections")]
    [Authorize(Policy = Policies.ClinicalRecordsManage)]
    public async Task<ActionResult<PatientMedicationResponse>> CorrectMedication(Guid id, [FromBody] CorrectMedicationRequest request, CancellationToken cancellationToken)
    {
        var command = new CorrectMedicationCommand(id, request.PatientId, request.MedicationName, request.Strength, request.DosageForm, request.Frequency, request.Route, request.StartDate, request.Indication, request.RecordedAtUtc);
        return Ok(ToMedicationResponse(await _medications.CorrectAsync(command, cancellationToken)));
    }

    [HttpGet("patients/{patientId:guid}/prescriptions")]
    [Authorize(Policy = Policies.PrescriptionsRead)]
    public async Task<ActionResult<IReadOnlyList<PrescriptionResponse>>> ListPrescriptions(Guid patientId, CancellationToken cancellationToken)
    => Ok((await _prescriptions.ListByPatientAsync(patientId, cancellationToken)).Select(ToPrescriptionResponse).ToList());

    [HttpPost("patients/{patientId:guid}/prescriptions")]
    [Authorize(Policy = Policies.PrescriptionsManage)]
    public async Task<ActionResult<PrescriptionResponse>> CreatePrescription(Guid patientId, [FromBody] CreatePrescriptionRequest request, CancellationToken cancellationToken)
    {
        var command = new CreatePrescriptionCommand(patientId, request.MedicationName, request.Strength, request.DosageForm, request.Frequency, request.Route, request.Quantity, request.RefillsAllowed, request.DaysSupply, request.Instructions, request.Note, request.RecordedAtUtc);
        return Ok(ToPrescriptionResponse(await _prescriptions.CreateAsync(command, cancellationToken)));
    }

    [HttpPut("prescriptions/{id:guid}")]
    [Authorize(Policy = Policies.PrescriptionsManage)]
    public async Task<ActionResult<PrescriptionResponse>> UpdatePrescription(Guid id, [FromBody] UpdatePrescriptionRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdatePrescriptionCommand(id, request.MedicationName, request.Strength, request.DosageForm, request.Frequency, request.Route, request.Quantity, request.RefillsAllowed, request.DaysSupply, request.Instructions, request.Note);
        return Ok(ToPrescriptionResponse(await _prescriptions.UpdateAsync(command, cancellationToken)));
    }

    [HttpPost("prescriptions/{id:guid}/issue")]
    [Authorize(Policy = Policies.PrescriptionsManage)]
    public async Task<ActionResult<PrescriptionResponse>> IssuePrescription(Guid id, CancellationToken cancellationToken)
        => Ok(ToPrescriptionResponse(await _prescriptions.IssueAsync(id, cancellationToken)));

    [HttpPost("prescriptions/{id:guid}/renew")]
    [Authorize(Policy = Policies.PrescriptionsManage)]
    public async Task<ActionResult<PrescriptionResponse>> RenewPrescription(Guid id, CancellationToken cancellationToken)
        => Ok(ToPrescriptionResponse(await _prescriptions.RenewAsync(id, cancellationToken)));

    [HttpPost("prescriptions/{id:guid}/cancel")]
    [Authorize(Policy = Policies.PrescriptionsManage)]
    public async Task<ActionResult<PrescriptionResponse>> CancelPrescription(Guid id, CancellationToken cancellationToken)
        => Ok(ToPrescriptionResponse(await _prescriptions.CancelAsync(id, cancellationToken)));

    [HttpPost("prescriptions/{id:guid}/dispense")]
    [Authorize(Policy = Permissions.PrescriptionsDispense)]
    public async Task<ActionResult<PrescriptionResponse>> DispensePrescription(Guid id, CancellationToken cancellationToken)
        => Ok(ToPrescriptionResponse(await _prescriptions.DispenseAsync(id, cancellationToken)));

    [HttpPost("prescriptions/{id:guid}/expire")]
    [Authorize(Policy = Permissions.PrescriptionsExpire)]
    public async Task<ActionResult<PrescriptionResponse>> ExpirePrescription(Guid id, CancellationToken cancellationToken)
        => Ok(ToPrescriptionResponse(await _prescriptions.ExpireAsync(id, cancellationToken)));

    [HttpPost("prescriptions/{id:guid}/corrections")]
    [Authorize(Policy = Policies.PrescriptionsManage)]
    public async Task<ActionResult<PrescriptionResponse>> CorrectPrescription(Guid id, [FromBody] CorrectPrescriptionRequest request, CancellationToken cancellationToken)
    {
        var command = new CorrectPrescriptionCommand(id, request.PatientId, request.MedicationName, request.Strength, request.DosageForm, request.Frequency, request.Route, request.Quantity, request.RefillsAllowed, request.DaysSupply, request.Instructions, request.Note, request.RecordedAtUtc);
        return Ok(ToPrescriptionResponse(await _prescriptions.CorrectAsync(command, cancellationToken)));
    }

    private static PrescriptionResponse ToPrescriptionResponse(PrescriptionDto d) => new(
        d.Id, d.PatientId, d.AuthorUserId, d.AuthorDoctorId, d.AuthorDisplayName,
        d.MedicationName, d.Strength, d.DosageForm, d.Frequency, d.Route, d.Quantity, d.RefillsAllowed, d.DaysSupply,
        d.Instructions, d.Note, d.Status, d.IsFinal, d.RecordedAtUtc, d.IssuedAt, d.IssuedByUserId,
        d.CancelledAt, d.CancelledByUserId, d.CorrectsPrescriptionId, d.CorrectsPrescriptionName, d.RenewedFromPrescriptionId, d.CreatedAt);

    [HttpGet("prescriptions/{id:guid}/documents")]
    [Authorize(Policy = Policies.DocumentsRead)]
    public async Task<ActionResult<IReadOnlyList<ClinicalDocumentResponse>>> ListPrescriptionDocuments(Guid id, CancellationToken cancellationToken)
    {
        var documents = await _documents.ListByPrescriptionAsync(id, cancellationToken);
        return Ok(documents.Select(ToClinicalDocumentResponse).ToList());
    }

    [HttpPost("prescriptions/{id:guid}/documents")]
    [Authorize(Policy = Policies.DocumentsManage)]
    public async Task<ActionResult<ClinicalDocumentResponse>> GeneratePrescriptionDocument(Guid id, CancellationToken cancellationToken)
    {
        var document = await _documents.GeneratePrescriptionDocumentAsync(id, cancellationToken);
        return Ok(ToClinicalDocumentResponse(document));
    }

    [HttpGet("documents/{id:guid}/download")]
    [Authorize(Policy = Policies.DocumentsRead)]
    public async Task<IActionResult> DownloadClinicalDocument(Guid id, CancellationToken cancellationToken)
    {
        var (content, fileName, contentType) = await _documents.DownloadAsync(id, cancellationToken);
        return File(content, contentType, fileName);
    }

    private static ClinicalDocumentResponse ToClinicalDocumentResponse(ClinicalDocumentModel d) => new(
        d.Id.ToString(), d.PrescriptionId.ToString(), d.FileName, d.ContentType, d.SizeBytes,
        d.GeneratedByUserId.ToString(), d.GeneratedAt.ToString("O"));

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

    private static VitalSignsMeasurements ToMeasurements(CreateVitalSignsRequest r) =>
        new(r.SystolicmmHg, r.DiastolicmmHg, r.HeartRateBpm, r.TemperatureC, r.TemperatureMethod, r.Position,
            r.RespiratoryRatePerMin, r.SpO2Percent, r.WeightKg, r.HeightCm, r.PainScore0to10, r.Note);
    private static VitalSignsMeasurements ToMeasurements(UpdateVitalSignsRequest r) =>
        new(r.SystolicmmHg, r.DiastolicmmHg, r.HeartRateBpm, r.TemperatureC, r.TemperatureMethod, r.Position,
            r.RespiratoryRatePerMin, r.SpO2Percent, r.WeightKg, r.HeightCm, r.PainScore0to10, r.Note);
    private static VitalSignsMeasurements ToMeasurements(CorrectVitalSignsRequest r) =>
        new(r.SystolicmmHg, r.DiastolicmmHg, r.HeartRateBpm, r.TemperatureC, r.TemperatureMethod, r.Position,
            r.RespiratoryRatePerMin, r.SpO2Percent, r.WeightKg, r.HeightCm, r.PainScore0to10, r.Note);

    private static VitalSignsResponse ToVitalSignsResponse(VitalSignsDto d) => new(
        d.Id, d.AppointmentId, d.AuthorUserId, d.AuthorDoctorId, d.AuthorDisplayName,
        d.SystolicmmHg, d.DiastolicmmHg, d.HeartRateBpm, d.TemperatureC, d.TemperatureMethod, d.Position,
        d.RespiratoryRatePerMin, d.SpO2Percent, d.WeightKg, d.HeightCm, d.PainScore0to10, d.Note,
        d.IsFinal, d.RecordedAtUtc, d.FinalizedAt, d.FinalizedByUserId, d.CorrectsVitalSignsId,
        d.CorrectsVitalSignsTitle, d.CreatedAt);

    private static DiagnosisResponse ToDiagnosisResponse(DiagnosisDto d) => new(
        d.Id, d.AppointmentId, d.AuthorUserId, d.AuthorDoctorId, d.AuthorDisplayName,
        d.Code, d.DisplayTerm, d.Category, d.OnsetDate, d.Note,
        d.IsFinal, d.RecordedAtUtc, d.FinalizedAt, d.FinalizedByUserId, d.CorrectsDiagnosisId,
        d.CorrectsDiagnosisTerm, d.CreatedAt);

    private static PatientAllergyResponse ToAllergyResponse(PatientAllergyDto d) => new(
        d.Id, d.PatientId, d.AuthorUserId, d.AuthorDoctorId, d.AuthorDisplayName,
        d.Allergen, d.AllergenClass, d.Severity, d.Reaction, d.OnsetDate,
        d.IsResolved, d.ResolvedAt, d.ResolvedByUserId,
        d.IsFinal, d.RecordedAtUtc, d.FinalizedAt, d.FinalizedByUserId, d.CorrectsAllergyId,
        d.CorrectsAllergyAllergen, d.CreatedAt);

    private static PatientMedicationResponse ToMedicationResponse(PatientMedicationDto d) => new(
        d.Id, d.PatientId, d.AuthorUserId, d.AuthorDoctorId, d.AuthorDisplayName,
        d.MedicationName, d.Strength, d.DosageForm, d.Frequency, d.Route, d.StartDate, d.Indication,
        d.Status, d.SuspendedAt, d.DiscontinuedAt, d.StatusChangedByUserId,
        d.IsFinal, d.RecordedAtUtc, d.FinalizedAt, d.FinalizedByUserId, d.CorrectsMedicationId, d.CorrectsMedicationName, d.CreatedAt);
}
