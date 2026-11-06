using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentValidation;
using MedicalAppointments.Application.Appointments.Auth;
using MedicalAppointments.Application.Appointments.Outbox;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Audit;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.Idempotency;
using MedicalAppointments.Application.MedicalStructure.Clinics;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Application.MedicalStructure.Patients;
using MedicalAppointments.Application.Scheduling.Availability;
using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Domain.Idempotency;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedicalAppointments.Application.Appointments;

public sealed class AppointmentBookingService : IAppointmentBookingService
{
    private const string CreateOperation = "appointments.create";
    private const string CancelOperation = "appointments.cancel";
    private const string RescheduleOperation = "appointments.reschedule";

    private readonly IValidator<CreateAppointmentCommand> _createValidator;
    private readonly IValidator<CancelAppointmentCommand> _cancelValidator;
    private readonly IValidator<RescheduleAppointmentCommand> _rescheduleValidator;

    private readonly IAppointmentAccessService _accessService;
    private readonly IAppointmentStore _appointmentStore;
    private readonly IPatientStore _patientStore;
    private readonly IDoctorStore _doctorStore;
    private readonly IClinicStore _clinicStore;
    private readonly IAvailabilityService _availabilityService;

    private readonly IBookingCoordinator _bookingCoordinator;
    private readonly IIdempotencyStore _idempotencyStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppointmentEventNotifier _eventNotifier;
    private readonly IAuditWriter _auditWriter;
    private readonly IRequestMetadataProvider _requestMetadata;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly TimeProvider _timeProvider;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly AppointmentBookingOptions _options;
    private readonly ILogger<AppointmentBookingService> _logger;

    public AppointmentBookingService(
        IValidator<CreateAppointmentCommand> createValidator,
        IValidator<CancelAppointmentCommand> cancelValidator,
        IValidator<RescheduleAppointmentCommand> rescheduleValidator,
        IAppointmentAccessService accessService,
        IAppointmentStore appointmentStore,
        IPatientStore patientStore,
        IDoctorStore doctorStore,
        IClinicStore clinicStore,
        IAvailabilityService availabilityService,
        IBookingCoordinator bookingCoordinator,
        IIdempotencyStore idempotencyStore,
        IUnitOfWork unitOfWork,
        IAppointmentEventNotifier eventNotifier,
        IAuditWriter auditWriter,
        IRequestMetadataProvider requestMetadata,
        ICorrelationIdProvider correlationIdProvider,
        TimeProvider timeProvider,
        JsonSerializerOptions jsonOptions,
        IOptions<AppointmentBookingOptions> optionsAccessor,
        ILogger<AppointmentBookingService> logger)
    {
        _createValidator = createValidator;
        _cancelValidator = cancelValidator;
        _rescheduleValidator = rescheduleValidator;
        _accessService = accessService;
        _appointmentStore = appointmentStore;
        _patientStore = patientStore;
        _doctorStore = doctorStore;
        _clinicStore = clinicStore;
        _availabilityService = availabilityService;
        _bookingCoordinator = bookingCoordinator;
        _idempotencyStore = idempotencyStore;
        _unitOfWork = unitOfWork;
        _eventNotifier = eventNotifier;
        _auditWriter = auditWriter;
        _requestMetadata = requestMetadata;
        _correlationIdProvider = correlationIdProvider;
        _timeProvider = timeProvider;
        _jsonOptions = jsonOptions;
        _options = optionsAccessor.Value;
        _logger = logger;
    }

    public async Task<IdempotentResult<AppointmentDto>> CreateAsync(
        CreateAppointmentCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ValidateIdempotencyKey(idempotencyKey);
        await _createValidator.ValidateAndThrowAsync(command, cancellationToken);

        var subject = await _accessService.GetSubjectAsync(cancellationToken);

        if (!_accessService.CanCreate(command, subject))
        {
            throw new ForbiddenException("You do not have permission to create this appointment.");
        }

        var requestHash = ComputeRequestHash(CreateOperation, command);
        var lockResources = BuildCreateLockResources(idempotencyKey, command);

        return await _bookingCoordinator.ExecuteInTransactionAsync(
            lockResources,
            async () => await ExecuteCreateAsync(command, idempotencyKey, requestHash, subject, cancellationToken),
            cancellationToken);
    }

    public async Task<IdempotentResult<AppointmentDto>> CancelAsync(
        CancelAppointmentCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ValidateIdempotencyKey(idempotencyKey);
        await _cancelValidator.ValidateAndThrowAsync(command, cancellationToken);

        var subject = await _accessService.GetSubjectAsync(cancellationToken);
        var requestHash = ComputeRequestHash(CancelOperation, command);

        var lockResources = new[]
        {
            IdempotencyLockResource(idempotencyKey),
            $"appointment:{command.AppointmentId}"
        };

        return await _bookingCoordinator.ExecuteInTransactionAsync(
            lockResources,
            async () => await ExecuteCancelAsync(command, idempotencyKey, requestHash, subject, cancellationToken),
            cancellationToken);
    }

    public async Task<IdempotentResult<RescheduleAppointmentResult>> RescheduleAsync(
        RescheduleAppointmentCommand command,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ValidateIdempotencyKey(idempotencyKey);
        await _rescheduleValidator.ValidateAndThrowAsync(command, cancellationToken);

        var subject = await _accessService.GetSubjectAsync(cancellationToken);
        var requestHash = ComputeRequestHash(RescheduleOperation, command);

        var oldAppointment = await _appointmentStore.GetTrackedByIdAsync(command.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        if (!_accessService.CanReschedule(oldAppointment, subject))
        {
            throw new ForbiddenException("You do not have permission to reschedule this appointment.");
        }

        var lockResources = BuildRescheduleLockResources(idempotencyKey, oldAppointment, command);

        return await _bookingCoordinator.ExecuteInTransactionAsync(
            lockResources,
            async () => await ExecuteRescheduleAsync(command, idempotencyKey, requestHash, subject, oldAppointment, cancellationToken),
            cancellationToken);
    }

    private async Task<IdempotentResult<AppointmentDto>> ExecuteCreateAsync(
        CreateAppointmentCommand command,
        string idempotencyKey,
        string requestHash,
        AppointmentAccessSubject subject,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();

        var existingIdempotency = await _idempotencyStore.FindAsync(idempotencyKey, cancellationToken);

        if (existingIdempotency is not null)
        {
            return HandleExistingIdempotency<AppointmentDto>(
                existingIdempotency,
                requestHash,
                _jsonOptions);
        }

        await ValidateReferencesAsync(command, cancellationToken);
        await ValidateSlotAsync(command.LocalDate, command.DoctorId, command.ClinicId, command.StartAtUtc, command.EndAtUtc, cancellationToken);
        await ValidateRoomAsync(command.RoomId, command.StartAtUtc, command.EndAtUtc, null, cancellationToken);
        ValidateAdvanceWindow(command.StartAtUtc, now);

        var patientUserId = await _patientStore.GetUserIdAsync(command.PatientId, cancellationToken);

        var appointment = Appointment.Create(
            command.PatientId,
            command.DoctorId,
            command.ClinicId,
            command.RoomId,
            command.LocalDate,
            command.StartAtUtc,
            command.EndAtUtc,
            command.Reason,
            command.Notes,
            originalAppointmentId: null,
            now: now);

        await _appointmentStore.AddAsync(appointment, cancellationToken);

        _eventNotifier.EnqueueCreated(appointment, patientUserId, now, _correlationIdProvider.CorrelationId);

        EnqueueAudit(
            subject.UserId,
            "Appointment.Created",
            appointment.Id,
            oldValues: null,
            newValues: SerializeAudit(new
            {
                status = appointment.Status.ToString(),
                startAt = appointment.StartAt,
                endAt = appointment.EndAt,
                doctorId = appointment.DoctorId,
                clinicId = appointment.ClinicId,
                roomId = appointment.RoomId
            }),
            now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await _appointmentStore.GetWithRelationsAsync(appointment.Id, cancellationToken)
            ?? throw new InvalidOperationException("Appointment was not found after creation.");

        var dto = AppointmentQueryService.Map(saved);

        var idempotencyRecord = IdempotencyRecord.Create(
            key: idempotencyKey,
            operation: CreateOperation,
            requestHash: requestHash,
            userId: subject.UserId,
            now: now,
            ttl: _options.IdempotencyTtl);

        idempotencyRecord.Complete(Serialize(dto), now);

        await _idempotencyStore.AddAsync(idempotencyRecord, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new IdempotentResult<AppointmentDto>(dto, false);
    }

    private async Task<IdempotentResult<AppointmentDto>> ExecuteCancelAsync(
        CancelAppointmentCommand command,
        string idempotencyKey,
        string requestHash,
        AppointmentAccessSubject subject,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();

        var existingIdempotency = await _idempotencyStore.FindAsync(idempotencyKey, cancellationToken);

        if (existingIdempotency is not null)
        {
            return HandleExistingIdempotency<AppointmentDto>(
                existingIdempotency,
                requestHash,
                _jsonOptions);
        }

        var appointment = await _appointmentStore.GetTrackedByIdAsync(command.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        if (!_accessService.CanCancel(appointment, subject))
        {
            throw new ForbiddenException("You do not have permission to cancel this appointment.");
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            var cancelledAppointmentDto = await GetDtoAsync(appointment.Id, cancellationToken);
            return new IdempotentResult<AppointmentDto>(cancelledAppointmentDto, true);
        }

        if (!subject.CanOverridePolicy)
        {
            var minCancellation = TimeSpan.FromHours(_options.MinCancellationHours);

            if (appointment.StartAt - now < minCancellation)
            {
                throw new PolicyViolationException(
                    $"Appointments cannot be cancelled less than {_options.MinCancellationHours} hour(s) before start.");
            }
        }

        var oldStatus = appointment.Status;
        var patientUserId = await _patientStore.GetUserIdAsync(appointment.PatientId, cancellationToken);

        appointment.Cancel(now, subject.UserId, command.Reason);

        _eventNotifier.EnqueueCancelled(appointment, patientUserId, oldStatus, now, _correlationIdProvider.CorrelationId);

        EnqueueAudit(
            subject.UserId,
            "Appointment.Cancelled",
            appointment.Id,
            SerializeAudit(new { status = oldStatus.ToString() }),
            SerializeAudit(new
            {
                status = appointment.Status.ToString(),
                cancelledAt = appointment.CancelledAt,
                cancelledByUserId = appointment.CancelledByUserId
            }),
            now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = await GetDtoAsync(appointment.Id, cancellationToken);

        var idempotencyRecord = IdempotencyRecord.Create(
            key: idempotencyKey,
            operation: CancelOperation,
            requestHash: requestHash,
            userId: subject.UserId,
            now: now,
            ttl: _options.IdempotencyTtl);

        idempotencyRecord.Complete(Serialize(dto), now);

        await _idempotencyStore.AddAsync(idempotencyRecord, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new IdempotentResult<AppointmentDto>(dto, false);
    }

    private async Task<IdempotentResult<RescheduleAppointmentResult>> ExecuteRescheduleAsync(
        RescheduleAppointmentCommand command,
        string idempotencyKey,
        string requestHash,
        AppointmentAccessSubject subject,
        Appointment oldAppointment,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();

        var existingIdempotency = await _idempotencyStore.FindAsync(idempotencyKey, cancellationToken);

        if (existingIdempotency is not null)
        {
            return HandleExistingIdempotency<RescheduleAppointmentResult>(
                existingIdempotency,
                requestHash,
                _jsonOptions);
        }

        if (!AppointmentStateMachine.CanTransition(oldAppointment.Status, AppointmentStatus.Rescheduled))
        {
            throw new Domain.Common.DomainException(
                $"Appointment cannot be rescheduled from status {oldAppointment.Status}.");
        }

        if (!subject.CanOverridePolicy)
        {
            var minReschedule = TimeSpan.FromHours(_options.MinRescheduleHours);

            if (oldAppointment.StartAt - now < minReschedule)
            {
                throw new PolicyViolationException(
                    $"Appointments cannot be rescheduled less than {_options.MinRescheduleHours} hour(s) before start.");
            }
        }

        var createCommand = new CreateAppointmentCommand(
            oldAppointment.PatientId,
            oldAppointment.DoctorId,
            oldAppointment.ClinicId,
            oldAppointment.RoomId,
            command.NewLocalDate,
            command.NewStartAtUtc,
            command.NewEndAtUtc,
            command.Reason ?? oldAppointment.Reason,
            oldAppointment.Notes);

        await ValidateReferencesAsync(createCommand, cancellationToken);
        await ValidateSlotAsync(
            createCommand.LocalDate,
            createCommand.DoctorId,
            createCommand.ClinicId,
            createCommand.StartAtUtc,
            createCommand.EndAtUtc,
            cancellationToken);

        await ValidateRoomAsync(
            createCommand.RoomId,
            createCommand.StartAtUtc,
            createCommand.EndAtUtc,
            excludeAppointmentId: null,
            cancellationToken);

        ValidateAdvanceWindow(createCommand.StartAtUtc, now);

        var patientUserId = await _patientStore.GetUserIdAsync(oldAppointment.PatientId, cancellationToken);

        var newAppointment = Appointment.Create(
            createCommand.PatientId,
            createCommand.DoctorId,
            createCommand.ClinicId,
            createCommand.RoomId,
            createCommand.LocalDate,
            createCommand.StartAtUtc,
            createCommand.EndAtUtc,
            createCommand.Reason,
            createCommand.Notes,
            originalAppointmentId: oldAppointment.Id,
            now: now);

        var oldStatus = oldAppointment.Status;

        oldAppointment.MarkRescheduled(now);

        await _appointmentStore.AddAsync(newAppointment, cancellationToken);

        _eventNotifier.EnqueueRescheduled(
            oldAppointment,
            newAppointment,
            patientUserId,
            oldStatus,
            now,
            _correlationIdProvider.CorrelationId);

        EnqueueAudit(
            subject.UserId,
            "Appointment.Rescheduled",
            oldAppointment.Id,
            SerializeAudit(new { status = oldStatus.ToString(), startAt = oldAppointment.StartAt }),
            SerializeAudit(new
            {
                status = oldAppointment.Status.ToString(),
                newAppointmentId = newAppointment.Id,
                newStartAt = newAppointment.StartAt
            }),
            now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var oldDto = await GetDtoAsync(oldAppointment.Id, cancellationToken);
        var newDto = await GetDtoAsync(newAppointment.Id, cancellationToken);
        var result = new RescheduleAppointmentResult(oldDto, newDto);

        var idempotencyRecord = IdempotencyRecord.Create(
            key: idempotencyKey,
            operation: RescheduleOperation,
            requestHash: requestHash,
            userId: subject.UserId,
            now: now,
            ttl: _options.IdempotencyTtl);

        idempotencyRecord.Complete(Serialize(result), now);

        await _idempotencyStore.AddAsync(idempotencyRecord, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new IdempotentResult<RescheduleAppointmentResult>(result, false);
    }

    private async Task ValidateReferencesAsync(
        CreateAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        if (!await _patientStore.ExistsAsync(command.PatientId, cancellationToken))
        {
            throw new NotFoundException("Patient not found.");
        }

        if (!await _doctorStore.ExistsAsync(command.DoctorId, cancellationToken))
        {
            throw new NotFoundException("Doctor not found.");
        }

        if (!await _clinicStore.ExistsAsync([command.ClinicId], cancellationToken))
        {
            throw new NotFoundException("Clinic not found.");
        }
    }

    private async Task ValidateSlotAsync(
        DateOnly localDate,
        Guid doctorId,
        Guid clinicId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken)
    {
        var availability = await _availabilityService.GetDayAsync(
            new AvailabilityQuery(doctorId, clinicId, localDate, null),
            cancellationToken);

        var slotExists = availability.Slots.Any(slot =>
            slot.StartAtUtc == startUtc &&
            slot.EndAtUtc == endUtc);

        if (!slotExists)
        {
            throw new ConflictException("The selected time slot is no longer available.");
        }
    }

    private async Task ValidateRoomAsync(
        Guid? roomId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        Guid? excludeAppointmentId,
        CancellationToken cancellationToken)
    {
        if (!roomId.HasValue)
        {
            return;
        }

        var overlap = await _appointmentStore.ExistsActiveRoomOverlapAsync(
            roomId.Value,
            startUtc,
            endUtc,
            excludeAppointmentId,
            cancellationToken);

        if (overlap)
        {
            throw new ConflictException("The selected room is not available for this time slot.");
        }
    }

    private void ValidateAdvanceWindow(DateTimeOffset startUtc, DateTimeOffset now)
    {
        if (startUtc <= now)
        {
            throw new PolicyViolationException("Appointments cannot be booked in the past.");
        }

        var maxAdvance = TimeSpan.FromDays(_options.MaxAdvanceDays);

        if (startUtc - now > maxAdvance)
        {
            throw new PolicyViolationException(
                $"Appointments cannot be booked more than {_options.MaxAdvanceDays} days in advance.");
        }
    }

    private async Task<AppointmentDto> GetDtoAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await _appointmentStore.GetWithRelationsAsync(id, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        return AppointmentQueryService.Map(entity);
    }

    private void EnqueueAudit(
        Guid? userId,
        string action,
        Guid entityId,
        string? oldValues,
        string? newValues,
        DateTimeOffset now)
    {
        _auditWriter.Enqueue(new AuditEntry(
            UserId: userId,
            Action: action,
            EntityName: nameof(Appointment),
            EntityId: entityId,
            Timestamp: now,
            IpAddress: _requestMetadata.IpAddress,
            UserAgent: _requestMetadata.UserAgent,
            OldValues: oldValues,
            NewValues: newValues,
            CorrelationId: _correlationIdProvider.CorrelationId));
    }

    private static IdempotentResult<T> HandleExistingIdempotency<T>(
        IdempotencyRecord record,
        string requestHash,
        JsonSerializerOptions jsonOptions)
    {
        if (record.Status == IdempotencyStatus.InProgress)
        {
            throw new ConflictException("Another request with the same idempotency key is in progress.");
        }

        if (record.Status == IdempotencyStatus.Failed)
        {
            throw new ConflictException("A previous request with the same idempotency key failed.");
        }

        if (!string.Equals(record.RequestHash, requestHash, StringComparison.Ordinal))
        {
            throw new ConflictException("Idempotency key was reused with a different request payload.");
        }

        if (string.IsNullOrWhiteSpace(record.ResponsePayload))
        {
            throw new InvalidOperationException("Idempotency record is completed but response payload is missing.");
        }

        var value = JsonSerializer.Deserialize<T>(record.ResponsePayload, jsonOptions)
            ?? throw new InvalidOperationException("Unable to deserialize idempotent response payload.");

        return new IdempotentResult<T>(value, true);
    }

    private static void ValidateIdempotencyKey(string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("Idempotency-Key header is required.", nameof(idempotencyKey));
        }

        if (idempotencyKey.Length > 128)
        {
            throw new ArgumentException("Idempotency-Key must not exceed 128 characters.", nameof(idempotencyKey));
        }
    }

    private string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, _jsonOptions);
    }

    private string SerializeAudit(object value)
    {
        return JsonSerializer.Serialize(value, _jsonOptions);
    }

    private static string ComputeRequestHash(string operation, object request)
    {
        var payload = JsonSerializer.Serialize(request, JsonDefaults.Options);
        var combined = $"{operation}|{payload}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(bytes);
    }

    private static string IdempotencyLockResource(string idempotencyKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(idempotencyKey));
        return $"idem:{Convert.ToHexString(bytes)}";
    }

    private static IEnumerable<string> BuildCreateLockResources(
        string idempotencyKey,
        CreateAppointmentCommand command)
    {
        yield return IdempotencyLockResource(idempotencyKey);

        foreach (var resource in BookingLockResources.ForInterval(
            command.DoctorId,
            command.ClinicId,
            command.RoomId,
            command.StartAtUtc,
            command.EndAtUtc))
        {
            yield return resource;
        }
    }

    private static IEnumerable<string> BuildRescheduleLockResources(
        string idempotencyKey,
        Appointment oldAppointment,
        RescheduleAppointmentCommand command)
    {
        yield return IdempotencyLockResource(idempotencyKey);
        yield return $"appointment:{oldAppointment.Id}";

        foreach (var resource in BookingLockResources.ForInterval(
            oldAppointment.DoctorId,
            oldAppointment.ClinicId,
            oldAppointment.RoomId,
            oldAppointment.StartAt,
            oldAppointment.EndAt))
        {
            yield return resource;
        }

        foreach (var resource in BookingLockResources.ForInterval(
            oldAppointment.DoctorId,
            oldAppointment.ClinicId,
            oldAppointment.RoomId,
            command.NewStartAtUtc,
            command.NewEndAtUtc))
        {
            yield return resource;
        }
    }
}