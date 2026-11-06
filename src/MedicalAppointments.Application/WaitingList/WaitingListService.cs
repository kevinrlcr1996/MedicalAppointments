using FluentValidation;
using MedicalAppointments.Application.Appointments;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Audit;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.MedicalStructure.Clinics;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Application.MedicalStructure.Patients;
using MedicalAppointments.Application.WaitingList.Common;
using MedicalAppointments.Application.WaitingList.Outbox;
using MedicalAppointments.Application.WaitingList.Stores;
using MedicalAppointments.Domain.WaitingList;

namespace MedicalAppointments.Application.WaitingList;

public sealed class WaitingListService : IWaitingListService
{
    private readonly IValidator<CreateWaitingListEntryCommand> _createValidator;
    private readonly IValidator<UpdateWaitingListEntryCommand> _updateValidator;
    private readonly IValidator<RespondOfferCommand> _respondValidator;

    private readonly IWaitingListAccessService _accessService;
    private readonly IWaitingListEntryStore _entryStore;
    private readonly IWaitingListOfferStore _offerStore;
    private readonly IPatientStore _patientStore;
    private readonly IDoctorStore _doctorStore;
    private readonly IClinicStore _clinicStore;
    private readonly IAppointmentBookingService _bookingService;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IWaitingListEventNotifier _notifier;
    private readonly IAuditWriter _auditWriter;
    private readonly IRequestMetadataProvider _requestMetadata;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly TimeProvider _timeProvider;

    public WaitingListService(
        IValidator<CreateWaitingListEntryCommand> createValidator,
        IValidator<UpdateWaitingListEntryCommand> updateValidator,
        IValidator<RespondOfferCommand> respondValidator,
        IWaitingListAccessService accessService,
        IWaitingListEntryStore entryStore,
        IWaitingListOfferStore offerStore,
        IPatientStore patientStore,
        IDoctorStore doctorStore,
        IClinicStore clinicStore,
        IAppointmentBookingService bookingService,
        IUnitOfWork unitOfWork,
        IWaitingListEventNotifier notifier,
        IAuditWriter auditWriter,
        IRequestMetadataProvider requestMetadata,
        ICorrelationIdProvider correlationIdProvider,
        TimeProvider timeProvider)
    {
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _respondValidator = respondValidator;
        _accessService = accessService;
        _entryStore = entryStore;
        _offerStore = offerStore;
        _patientStore = patientStore;
        _doctorStore = doctorStore;
        _clinicStore = clinicStore;
        _bookingService = bookingService;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _auditWriter = auditWriter;
        _requestMetadata = requestMetadata;
        _correlationIdProvider = correlationIdProvider;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<WaitingListEntryDto>> GetEntriesAsync(
        WaitingListEntryQuery query,
        CancellationToken cancellationToken = default)
    {
        var subject = await _accessService.GetSubjectAsync(cancellationToken);
        var filter = _accessService.BuildEntryReadFilter(subject);

        if (filter.Denied)
        {
            throw new ForbiddenException("You do not have access to the waiting list.");
        }

        var result = await _entryStore.GetPagedAsync(query, filter, cancellationToken);

        return new PagedResult<WaitingListEntryDto>(
            result.Items.Select(MapEntry).ToList(),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages);
    }

    public async Task<WaitingListEntryDto> GetEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subject = await _accessService.GetSubjectAsync(cancellationToken);
        var entity = await _entryStore.GetWithPatientAsync(id, cancellationToken)
            ?? throw new NotFoundException("Waiting list entry not found.");

        var dto = MapEntry(entity);
        if (!_accessService.CanReadEntry(dto, subject))
        {
            throw new ForbiddenException("You do not have access to this entry.");
        }

        return dto;
    }

    public async Task<WaitingListEntryDto> CreateEntryAsync(
        CreateWaitingListEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(command, cancellationToken);

        var subject = await _accessService.GetSubjectAsync(cancellationToken);
        if (!_accessService.CanCreateEntry(command, subject))
        {
            throw new ForbiddenException("You do not have permission to create this entry.");
        }

        await ValidateRefsAsync(command.PatientId, command.DoctorId, command.ClinicId, cancellationToken);

        var now = _timeProvider.GetUtcNow();

        var entity = WaitingListEntry.Create(
            command.PatientId,
            command.DoctorId,
            command.ClinicId,
            command.PreferredFromUtc,
            command.PreferredToUtc,
            command.Priority,
            command.Note,
            now);

        await _entryStore.AddAsync(entity, cancellationToken);

        var patientUserId = await _patientStore.GetUserIdAsync(command.PatientId, cancellationToken);
        _notifier.EnqueueUpdated(
            entity.Id, null, entity.PatientId, patientUserId, entity.DoctorId, entity.ClinicId,
            "EntryCreated", entity.Status, null, now, _correlationIdProvider.CorrelationId);

        EnqueueAudit(subject.UserId, "WaitingList.EntryCreated", entity.Id, null,
            SerializeNew(new { doctorId = entity.DoctorId, clinicId = entity.ClinicId, priority = entity.Priority.ToString() }), now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Recarga con relaciones para que MapEntry devuelva nombres reales (no vacíos).
        var saved = await _entryStore.GetWithPatientAsync(entity.Id, cancellationToken) ?? entity;
        return MapEntry(saved);
    }

    public async Task<WaitingListEntryDto> UpdateEntryAsync(
        UpdateWaitingListEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(command, cancellationToken);

        var subject = await _accessService.GetSubjectAsync(cancellationToken);
        var entity = await _entryStore.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Waiting list entry not found.");

        var currentDto = MapEntry(entity);
        if (!_accessService.CanManageEntry(currentDto, subject))
        {
            throw new ForbiddenException("You do not have permission to edit this entry.");
        }

        await ValidateRefsAsync(entity.PatientId, entity.DoctorId, command.ClinicId, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        entity.UpdateWindow(command.ClinicId, command.PreferredFromUtc, command.PreferredToUtc, command.Priority, command.Note, now);

        var patientUserId = await _patientStore.GetUserIdAsync(entity.PatientId, cancellationToken);
        _notifier.EnqueueUpdated(
            entity.Id, null, entity.PatientId, patientUserId, entity.DoctorId, entity.ClinicId,
            "EntryUpdated", entity.Status, null, now, _correlationIdProvider.CorrelationId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await _entryStore.GetWithPatientAsync(entity.Id, cancellationToken) ?? entity;
        return MapEntry(saved);
    }

    public async Task<WaitingListEntryDto> CancelEntryAsync(
        CancelWaitingListEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        var subject = await _accessService.GetSubjectAsync(cancellationToken);
        var entity = await _entryStore.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Waiting list entry not found.");

        var currentDto = MapEntry(entity);
        if (!_accessService.CanManageEntry(currentDto, subject))
        {
            throw new ForbiddenException("You do not have permission to cancel this entry.");
        }

        var now = _timeProvider.GetUtcNow();
        entity.Cancel(now);

        var patientUserId = await _patientStore.GetUserIdAsync(entity.PatientId, cancellationToken);
        _notifier.EnqueueUpdated(
            entity.Id, null, entity.PatientId, patientUserId, entity.DoctorId, entity.ClinicId,
            "EntryCancelled", entity.Status, null, now, _correlationIdProvider.CorrelationId);

        EnqueueAudit(subject.UserId, "WaitingList.EntryCancelled", entity.Id, null, null, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await _entryStore.GetWithPatientAsync(entity.Id, cancellationToken) ?? entity;
        return MapEntry(saved);
    }

    public async Task<PagedResult<WaitingListOfferDto>> GetOffersAsync(
        WaitingListOfferQuery query,
        CancellationToken cancellationToken = default)
    {
        var subject = await _accessService.GetSubjectAsync(cancellationToken);

        IReadOnlyCollection<Guid>? patientIds = null;
        IReadOnlyCollection<Guid>? doctorIds = null;
        var all = subject.CanReadAll;

        if (!all)
        {
            patientIds = subject.CanReadOwn && subject.PatientId.HasValue ? new[] { subject.PatientId.Value } : Array.Empty<Guid>();
            doctorIds = subject.CanReadAssigned && subject.DoctorId.HasValue ? new[] { subject.DoctorId.Value } : Array.Empty<Guid>();

            if (patientIds.Count == 0 && doctorIds.Count == 0)
            {
                return new PagedResult<WaitingListOfferDto>([], query.Page, query.PageSize, 0, 0);
            }
        }

        var result = await _offerStore.GetPagedAsync(query, patientIds, doctorIds, all, cancellationToken);

        return new PagedResult<WaitingListOfferDto>(
            result.Items.Select(MapOffer).ToList(),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages);
    }

    public async Task<WaitingListOfferDto> RespondOfferAsync(
        RespondOfferCommand command,
        CancellationToken cancellationToken = default)
    {
        await _respondValidator.ValidateAndThrowAsync(command, cancellationToken);

        var subject = await _accessService.GetSubjectAsync(cancellationToken);

        // GetTrackedByIdAsync de oferta ya incluye Doctor y Clinic => MapOffer tiene datos.
        var offer = await _offerStore.GetTrackedByIdAsync(command.OfferId, cancellationToken)
            ?? throw new NotFoundException("Offer not found.");

        var offerDto = MapOffer(offer);
        if (!_accessService.CanRespondOffer(offerDto, subject))
        {
            throw new ForbiddenException("You do not have permission to respond to this offer.");
        }

        var now = _timeProvider.GetUtcNow();

        if (!offer.IsPendingAndAlive(now))
        {
            throw new ConflictException("This offer is no longer pending or has expired.");
        }

        if (command.Accept)
        {
            // REUSO DEL KERNEL DE FASE 5: app-locks + revalidación + idempotency + auditoría de cita.
            var bookingCommand = new CreateAppointmentCommand(
                offer.PatientId,
                offer.DoctorId,
                offer.ClinicId ?? throw new ConflictException("Offer has no clinic."),
                offer.RoomId,
                offer.LocalDate,
                offer.SlotStartUtc,
                offer.SlotEndUtc,
                Reason: "Reserved from waiting list",
                Notes: null);

            // Idempotency key estable por oferta: reintentos no duplican la cita.
            var idempotencyKey = $"wl-offer:{offer.Id}";

            try
            {
                await _bookingService.CreateAsync(bookingCommand, idempotencyKey, cancellationToken);
            }
            catch (ConflictException)
            {
                // El slot se tomó entre la oferta y la aceptación: expira la oferta y avisa al siguiente.
                offer.MarkExpired(now);
                var pUserId = await _patientStore.GetUserIdAsync(offer.PatientId, cancellationToken);
                _notifier.EnqueueUpdated(
                    offer.EntryId, offer.Id, offer.PatientId, pUserId, offer.DoctorId, offer.ClinicId,
                    "OfferExpiredByConflict", null, offer.Status, now, _correlationIdProvider.CorrelationId);
                _notifier.EnqueueSlotFreed(
                    offer.DoctorId, offer.ClinicId, offer.RoomId, offer.LocalDate,
                    offer.SlotStartUtc, offer.SlotEndUtc, now, _correlationIdProvider.CorrelationId);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                throw new ConflictException("The time slot was already taken. The offer has expired.");
            }

            offer.MarkAccepted(now);
            var entry = await _entryStore.GetTrackedByIdAsync(offer.EntryId, cancellationToken);
            entry?.MarkFulfilled(now);

            var patientUserId = await _patientStore.GetUserIdAsync(offer.PatientId, cancellationToken);
            _notifier.EnqueueUpdated(
                offer.EntryId, offer.Id, offer.PatientId, patientUserId, offer.DoctorId, offer.ClinicId,
                "OfferAccepted", entry?.Status, offer.Status, now, _correlationIdProvider.CorrelationId);

            EnqueueAudit(subject.UserId, "WaitingList.OfferAccepted", offer.Id, null,
                SerializeNew(new { appointmentBooked = true }), now);
        }
        else
        {
            offer.MarkDeclined(now);
            var patientUserId = await _patientStore.GetUserIdAsync(offer.PatientId, cancellationToken);
            _notifier.EnqueueUpdated(
                offer.EntryId, offer.Id, offer.PatientId, patientUserId, offer.DoctorId, offer.ClinicId,
                "OfferDeclined", null, offer.Status, now, _correlationIdProvider.CorrelationId);

            // Declinar libera la oferta => re-matching al siguiente candidato para ese slot.
            _notifier.EnqueueSlotFreed(
                offer.DoctorId, offer.ClinicId, offer.RoomId, offer.LocalDate,
                offer.SlotStartUtc, offer.SlotEndUtc, now, _correlationIdProvider.CorrelationId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapOffer(offer);
    }

    private async Task ValidateRefsAsync(
        Guid patientId, Guid doctorId, Guid? clinicId, CancellationToken cancellationToken)
    {
        if (!await _patientStore.ExistsAsync(patientId, cancellationToken))
        {
            throw new NotFoundException("Patient not found.");
        }

        if (!await _doctorStore.ExistsAsync(doctorId, cancellationToken))
        {
            throw new NotFoundException("Doctor not found.");
        }

        if (clinicId.HasValue && !await _clinicStore.ExistsAsync([clinicId.Value], cancellationToken))
        {
            throw new NotFoundException("Clinic not found.");
        }
    }

    private void EnqueueAudit(Guid? userId, string action, Guid entityId, string? oldValues, string? newValues, DateTimeOffset now)
    {
        _auditWriter.Enqueue(new AuditEntry(
            userId, action, nameof(WaitingListEntry), entityId, now,
            _requestMetadata.IpAddress, _requestMetadata.UserAgent, oldValues, newValues,
            _correlationIdProvider.CorrelationId));
    }

    private string SerializeNew(object value) =>
        System.Text.Json.JsonSerializer.Serialize(value, JsonDefaults.Options);

    private static WaitingListEntryDto MapEntry(WaitingListEntry e) => new(
        e.Id, e.PatientId,
        e.Patient is null ? string.Empty : $"{e.Patient.FirstName} {e.Patient.LastName}",
        e.DoctorId,
        e.Doctor is null ? string.Empty : $"{e.Doctor.FirstName} {e.Doctor.LastName}",
        e.ClinicId, e.Clinic?.Name,
        e.PreferredFromUtc, e.PreferredToUtc, e.Priority, e.Status, e.Note,
        e.CreatedAt, e.UpdatedAt);

    private static WaitingListOfferDto MapOffer(WaitingListOffer o) => new(
        o.Id, o.EntryId, o.PatientId, o.DoctorId,
        o.Doctor is null ? string.Empty : $"{o.Doctor.FirstName} {o.Doctor.LastName}",
        o.ClinicId, o.Clinic?.Name, o.RoomId, o.LocalDate,
        o.SlotStartUtc, o.SlotEndUtc, o.Status, o.ExpiresAt, o.RespondedAt,
        o.CreatedAt, o.UpdatedAt);
}