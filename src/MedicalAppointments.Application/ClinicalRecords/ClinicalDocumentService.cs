using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using MedicalAppointments.Application.ClinicalRecords.Auth;
using MedicalAppointments.Application.ClinicalRecords.Common;
using MedicalAppointments.Application.ClinicalRecords.Outbox;
using MedicalAppointments.Application.ClinicalRecords.Storage;
using MedicalAppointments.Application.ClinicalRecords.Stores;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Audit;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.Outbox;
using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords;

public sealed class ClinicalDocumentService : IClinicalDocumentService
{
    private const string HtmlContentType = "text/html; charset=utf-8";
    private readonly IPrescriptionsAccessService _access;
    private readonly IPrescriptionStore _prescriptions;
    private readonly IClinicalDocumentStore _documents;
    private readonly IFileStorage _storage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClinicalRecordsEventNotifier _notifier;
    private readonly IAuditWriter _auditWriter;
    private readonly IRequestMetadataProvider _requestMetadata;
    private readonly ICorrelationIdProvider _correlationId;
    private readonly TimeProvider _timeProvider;

    public ClinicalDocumentService(
        IPrescriptionsAccessService access,
        IPrescriptionStore prescriptions,
        IClinicalDocumentStore documents,
        IFileStorage storage,
        IUnitOfWork unitOfWork,
        IClinicalRecordsEventNotifier notifier,
        IAuditWriter auditWriter,
        IRequestMetadataProvider requestMetadata,
        ICorrelationIdProvider correlationId,
        TimeProvider timeProvider)
    {
        _access = access;
        _prescriptions = prescriptions;
        _documents = documents;
        _storage = storage;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _auditWriter = auditWriter;
        _requestMetadata = requestMetadata;
        _correlationId = correlationId;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<ClinicalDocumentModel>> ListByPrescriptionAsync(Guid prescriptionId, CancellationToken cancellationToken = default)
    {
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var prescription = await LoadPrescriptionAsync(prescriptionId, cancellationToken);
        EnsureReadAccess(prescription, subject);
        return (await _documents.GetByPrescriptionAsync(prescriptionId, cancellationToken)).Select(Map).ToArray();
    }

    public async Task<ClinicalDocumentModel> GeneratePrescriptionDocumentAsync(Guid prescriptionId, CancellationToken cancellationToken = default)
    {
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var prescription = await LoadPrescriptionAsync(prescriptionId, cancellationToken);
        if (!_access.CanGenerateDocuments(prescription, subject))
            throw new ForbiddenException("You do not have permission to generate this prescription document.");
        if (!subject.UserId.HasValue)
            throw new ConflictException("An authenticated user is required to generate a clinical document.");
        if (prescription.Status is not PrescriptionStatus.Issued and not PrescriptionStatus.Dispensed)
            throw new ConflictException("A document can only be generated from an issued prescription.");

        var now = _timeProvider.GetUtcNow();
        var html = RenderPrescriptionHtml(prescription);
        var bytes = Encoding.UTF8.GetBytes(html);
        var storageKey = $"clinical-documents/{Guid.CreateVersion7():N}.html";
        var fileName = $"receta-{prescription.Id:N}.html";
        await using var content = new MemoryStream(bytes, writable: false);
        var stored = await _storage.SaveAsync(content, storageKey, bytes.Length, cancellationToken);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var document = ClinicalDocument.Create(prescription.Id, fileName, HtmlContentType, stored.SizeBytes,
            stored.StorageKey, hash, subject.UserId.Value, now);

        try
        {
            await _documents.AddAsync(document, cancellationToken);
            _notifier.EnqueueClinicalDocumentEvent(document, prescription.PatientId, now, _correlationId.CorrelationId);
            _auditWriter.Enqueue(new AuditEntry(subject.UserId, "ClinicalRecords.DocumentGenerated", nameof(ClinicalDocument),
                document.Id, now, _requestMetadata.IpAddress, _requestMetadata.UserAgent, null,
                System.Text.Json.JsonSerializer.Serialize(new { prescriptionId = prescription.Id }), _correlationId.CorrelationId));
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _storage.DeleteAsync(stored.StorageKey, CancellationToken.None);
            throw;
        }

        return Map(document);
    }

    public async Task<(Stream Content, string FileName, string ContentType)> DownloadAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var subject = await _access.GetSubjectAsync(cancellationToken);
        var document = await _documents.GetTrackedByIdAsync(documentId, cancellationToken)
            ?? throw new NotFoundException("Clinical document not found.");
        var prescription = await LoadPrescriptionAsync(document.PrescriptionId, cancellationToken);
        EnsureReadAccess(prescription, subject);
        var stream = await _storage.OpenReadAsync(document.StorageKey, cancellationToken);
        return (stream, document.FileName, document.ContentType);
    }

    private async Task<Prescription> LoadPrescriptionAsync(Guid id, CancellationToken cancellationToken)
        => await _prescriptions.GetTrackedByIdWithPatientAsync(id, cancellationToken)
            ?? throw new NotFoundException("Prescription not found.");

    private void EnsureReadAccess(Prescription prescription, PrescriptionsAccessSubject subject)
    {
        if (!_access.CanReadPrescriptions(subject) || !_access.CanReadDocuments(prescription, subject))
            throw new ForbiddenException("You do not have access to this prescription document.");
    }

    private static ClinicalDocumentModel Map(ClinicalDocument document)
        => new(document.Id, document.PrescriptionId, document.FileName, document.ContentType,
            document.SizeBytes, document.GeneratedByUserId, document.GeneratedAt);

    private static string RenderPrescriptionHtml(Prescription p)
    {
        static string E(string? value) => HtmlEncoder.Default.Encode(value ?? string.Empty);
        var patientName = $"{p.Patient.FirstName} {p.Patient.LastName}".Trim();
        var issuedDate = (p.IssuedAt ?? p.RecordedAtUtc).ToString("yyyy-MM-dd HH:mm 'UTC'");
        var details = new (string Label, string? Value)[]
        {
            ("Medicamento", string.Join(" ", new[] { p.MedicationName, p.Strength }.Where(x => !string.IsNullOrWhiteSpace(x)))),
            ("Forma farmacéutica", p.DosageForm), ("Vía", p.Route), ("Frecuencia", p.Frequency),
            ("Cantidad", p.Quantity?.ToString()), ("Días de suministro", p.DaysSupply?.ToString()),
            ("Renovaciones permitidas", p.RefillsAllowed?.ToString()), ("Instrucciones", p.Instructions)
        };
        var rows = string.Join(Environment.NewLine, details.Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"<tr><th>{E(x.Label)}</th><td>{E(x.Value)}</td></tr>"));

        return """
            <!doctype html>
            <html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
            <title>Receta médica</title><style>
            body{font:16px/1.5 Arial,sans-serif;color:#172033;max-width:760px;margin:40px auto;padding:0 24px}
            h1{font-size:26px;border-bottom:2px solid #2563eb;padding-bottom:12px}p{margin:8px 0}
            table{border-collapse:collapse;width:100%;margin-top:24px}th,td{text-align:left;padding:12px;border-bottom:1px solid #dbe2ea;vertical-align:top}
            th{width:34%;color:#475569}.signature{margin-top:56px;border-top:1px solid #64748b;width:280px;padding-top:8px}
            @media print{body{margin:0 auto;padding:0 16px}}
            </style></head><body>
            <h1>Receta médica</h1>
            <p><strong>Paciente:</strong> {{patient}}</p>
            <p><strong>Profesional:</strong> {{author}}</p>
            <p><strong>Emitida:</strong> {{issued}}</p>
            <table><tbody>{{rows}}</tbody></table>
            <p class="signature">{{author}}<br>Receta emitida electrónicamente</p>
            </body></html>
            """
            .Replace("{{patient}}", E(patientName), StringComparison.Ordinal)
            .Replace("{{author}}", E(p.AuthorDisplayName), StringComparison.Ordinal)
            .Replace("{{issued}}", E(issuedDate), StringComparison.Ordinal)
            .Replace("{{rows}}", rows, StringComparison.Ordinal);
    }
}
