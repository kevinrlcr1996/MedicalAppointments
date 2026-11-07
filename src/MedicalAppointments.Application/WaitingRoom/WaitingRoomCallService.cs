using MedicalAppointments.Application.Appointments;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Audit;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Application.MedicalStructure.Patients;
using MedicalAppointments.Application.MedicalStructure.Rooms;
using MedicalAppointments.Application.WaitingRoom.Auth;
using MedicalAppointments.Application.WaitingRoom.Common;
using MedicalAppointments.Application.WaitingRoom.Outbox;
using MedicalAppointments.Application.WaitingRoom.Stores;
using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Domain.WaitingRoom;
using Microsoft.Extensions.Logging;

namespace MedicalAppointments.Application.WaitingRoom;

public sealed class WaitingRoomCallService : IWaitingRoomCallService
{
    private readonly IWaitingRoomCallStore _store;
    private readonly IWaitingRoomAccessService _access;
    private readonly IAppointmentStore _appointmentStore;
    private readonly IAppointmentWorkflowService _workflow;
    private readonly IPatientStore _patientStore;
    private readonly IDoctorStore _doctorStore;
    private readonly IRoomStore _roomStore;
    private readonly IBookingCoordinator _coordinator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWaitingRoomEventNotifier _notifier;
    private readonly IAuditWriter _auditWriter;
    private readonly IRequestMetadataProvider _requestMetadata;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<WaitingRoomCallService> _logger;

    public WaitingRoomCallService(
        IWaitingRoomCallStore store,
        IWaitingRoomAccessService access,
        IAppointmentStore appointmentStore,
        IAppointmentWorkflowService workflow,
        IPatientStore patientStore,
        IDoctorStore doctorStore,
        IRoomStore roomStore,
        IBookingCoordinator coordinator,
        IUnitOfWork unitOfWork,
        IWaitingRoomEventNotifier notifier,
        IAuditWriter auditWriter,
        IRequestMetadataProvider requestMetadata,
        ICorrelationIdProvider correlationIdProvider,
        TimeProvider timeProvider,
        ILogger<WaitingRoomCallService> logger)
    {
        _store = store;
        _access = access;
        _appointmentStore = appointmentStore;
        _workflow = workflow;
        _patientStore = patientStore;
        _doctorStore = doctorStore;
        _roomStore = roomStore;
        _coordinator = coordinator;
        _unitOfWork = unitOfWork;
        _notifier = notifier;
        _auditWriter = auditWriter;
        _requestMetadata = requestMetadata;
        _correlationIdProvider = correlationIdProvider;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<WaitingRoomCardDto> CheckInToRoomAsync(
        CheckInToRoomCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);

        var appointment = await _appointmentStore.GetWithRelationsAsync(command.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        if (appointment.Status != AppointmentStatus.CheckedIn)
        {
            throw new ConflictException("The patient must be checked in before entering the waiting room.");
        }

        if (!_access.CanOperateQueue(appointment.DoctorId, subject))
        {
            throw new ForbiddenException("You do not have permission to operate this waiting room.");
        }

        if (await _store.ExistsByAppointmentAsync(command.AppointmentId, cancellationToken))
        {
            throw new ConflictException("This appointment is already in the waiting room.");
        }

        var lockResource = QueueLockResource(appointment.DoctorId, appointment.ClinicId, appointment.LocalDate);

        return await _coordinator.ExecuteInTransactionAsync(
            [lockResource],
            async () =>
            {
                var sequence = await _store.GetNextSequenceAsync(
                    appointment.DoctorId, appointment.ClinicId, appointment.LocalDate, cancellationToken);

                var call = WaitingRoomCall.Queue(
                    appointment.Id,
                    appointment.DoctorId,
                    appointment.ClinicId,
                    appointment.LocalDate,
                    sequence,
                    command.Note,
                    now);

                await _store.AddAsync(call, cancellationToken);

                _notifier.EnqueueUpdated(call, "Queued", now, _correlationIdProvider.CorrelationId);
                EnqueueAudit(subject.UserId, "WaitingRoom.Queued", call.Id, now,
                    new { appointmentId = appointment.Id, sequence });

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return await BuildCardAsync(call, cancellationToken);
            },
            cancellationToken);
    }

    public async Task<WaitingRoomCardDto> CallNextAsync(
        CallNextCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);

        if (!_access.CanOperateQueue(command.DoctorId, subject))
        {
            throw new ForbiddenException("You do not have permission to operate this waiting room.");
        }

        if (command.RoomId.HasValue)
        {
            var room = await _roomStore.GetTrackedByIdAsync(command.RoomId.Value, cancellationToken);
            if (room is null)
            {
                throw new NotFoundException("Room not found.");
            }
        }

        var lockResource = QueueLockResource(command.DoctorId, command.ClinicId, command.LocalDate);

        var (card, hadTarget) = await _coordinator.ExecuteInTransactionAsync(
            [lockResource],
            async () =>
            {
                var queued = await _store.GetQueuedCallsTrackedOrderedAsync(
                    command.DoctorId, command.ClinicId, command.LocalDate, cancellationToken);

                WaitingRoomCall? target = null;

                foreach (var call in queued)
                {
                    var appointment = await _appointmentStore.GetTrackedByIdAsync(call.AppointmentId, cancellationToken);

                    if (appointment is null)
                    {
                        ReconcileCall(call, AppointmentStatus.Cancelled, now);
                        continue;
                    }

                    if (appointment.Status == AppointmentStatus.CheckedIn)
                    {
                        target = call;
                        break;
                    }

                    ReconcileCall(call, appointment.Status, now);
                }

                if (target is null)
                {
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return ((WaitingRoomCardDto?)null, false);
                }

                var targetAppointment = await _appointmentStore.GetTrackedByIdAsync(target.AppointmentId, cancellationToken)
                    ?? throw new NotFoundException("Appointment not found.");

                target.MarkCalled(command.RoomId, now);

                var builtCard = await BuildCardAsync(target, cancellationToken);

                _notifier.EnqueuePatientCalled(
                    target,
                    targetAppointment.PatientId,
                    await _patientStore.GetUserIdAsync(targetAppointment.PatientId, cancellationToken),
                    builtCard.PatientFullName,
                    builtCard.DoctorFullName,
                    builtCard.RoomName,
                    now,
                    _correlationIdProvider.CorrelationId);

                _notifier.EnqueueUpdated(target, "Called", now, _correlationIdProvider.CorrelationId);
                EnqueueAudit(subject.UserId, "WaitingRoom.Called", target.Id, now,
                    new { appointmentId = target.AppointmentId, roomId = command.RoomId });

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return (builtCard as WaitingRoomCardDto, true);
            },
            cancellationToken);

        if (!hadTarget)
        {
            throw new ConflictException("There are no patients waiting in this queue.");
        }

        return card!;
    }

    public Task<WaitingRoomCardDto> AnswerAsync(AnswerCallCommand command, CancellationToken cancellationToken = default)
    {
        return TransitionAsync(
            command.CallId,
            call => call.MarkAnsweredOrThrow(),
            (call, now) => call.MarkInConsultation(now),
            (appointment, subject) => _access.CanAnswer(appointment, subject),
            (appointment, ct) => _workflow.StartAsync(appointment.Id, ct),
            "InConsultation",
            cancellationToken);
    }

    public Task<WaitingRoomCardDto> SkipAsync(SkipCallCommand command, CancellationToken cancellationToken = default)
    {
        return TransitionAsync(
            command.CallId,
            call => call.EnsureCalled(),
            (call, now) => call.MarkSkipped(now),
            (appointment, subject) => _access.CanOperateQueue(appointment.DoctorId, subject),
            null,
            "Skipped",
            cancellationToken,
            extraAudit: new { reason = command.Reason });
    }

    public Task<WaitingRoomCardDto> RecallAsync(RecallCallCommand command, CancellationToken cancellationToken = default)
    {
        return TransitionAsync(
            command.CallId,
            call => call.EnsureCalledOrSkipped(), // FIX: permitir recall desde Skipped (coherente con UI y dominio)
            (call, now) => call.MarkRecalled(now),
            (appointment, subject) => _access.CanOperateQueue(appointment.DoctorId, subject),
            null,
            "Recalled",
            cancellationToken);
    }

    public Task<WaitingRoomCardDto> CompleteAsync(CompleteCallCommand command, CancellationToken cancellationToken = default)
    {
        return TransitionAsync(
            command.CallId,
            call => call.EnsureInConsultationOrCalled(),
            (call, now) => call.MarkCompleted(now),
            (appointment, subject) => _access.CanComplete(appointment, subject),
            (appointment, ct) => _workflow.CompleteAsync(appointment.Id, ct),
            "Completed",
            cancellationToken);
    }

    private async Task<WaitingRoomCardDto> TransitionAsync(
        Guid callId,
        Action<WaitingRoomCall> guard,
        Action<WaitingRoomCall, DateTimeOffset> mutate,
        Func<Appointment, WaitingRoomAccessSubject, bool> authorize,
        Func<Appointment, CancellationToken, Task>? workflowAction,
        string changeType,
        CancellationToken cancellationToken,
        object? extraAudit = null)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _access.GetSubjectAsync(cancellationToken);

        var call = await _store.GetTrackedByIdAsync(callId, cancellationToken)
            ?? throw new NotFoundException("Waiting room call not found.");

        var appointment = await _appointmentStore.GetWithRelationsAsync(call.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        if (!authorize(appointment, subject))
        {
            throw new ForbiddenException("You do not have permission for this waiting room action.");
        }

        guard(call);

        if (workflowAction is not null)
        {
            await workflowAction(appointment, cancellationToken);
        }

        mutate(call, now);

        if (changeType == "Recalled")
        {
            _notifier.EnqueuePatientRecalled(
                call,
                appointment.PatientId,
                await _patientStore.GetUserIdAsync(appointment.PatientId, cancellationToken),
                appointment.Patient.FullName(),
                null,
                now,
                _correlationIdProvider.CorrelationId);
        }

        _notifier.EnqueueUpdated(call, changeType, now, _correlationIdProvider.CorrelationId);
        EnqueueAudit(subject.UserId, $"WaitingRoom.{changeType}", call.Id, now, extraAudit);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuildCardAsync(call, cancellationToken);
    }

    private static void ReconcileCall(WaitingRoomCall call, AppointmentStatus appointmentStatus, DateTimeOffset now)
    {
        switch (appointmentStatus)
        {
            case AppointmentStatus.InProgress:
                if (call.Status == WaitingRoomCallStatus.Queued)
                {
                    call.MarkCalled(null, now);
                }

                if (call.Status == WaitingRoomCallStatus.Called)
                {
                    call.MarkInConsultation(now);
                }

                break;

            case AppointmentStatus.Completed:
                if (call.Status == WaitingRoomCallStatus.Queued)
                {
                    call.MarkCalled(null, now);
                }

                if (call.Status is WaitingRoomCallStatus.Called or WaitingRoomCallStatus.InConsultation)
                {
                    call.MarkCompleted(now);
                }

                break;

            case AppointmentStatus.Cancelled:
            case AppointmentStatus.NoShow:
            case AppointmentStatus.Rescheduled:
                if (call.Status == WaitingRoomCallStatus.Queued)
                {
                    call.MarkCalled(null, now);
                }

                if (call.Status == WaitingRoomCallStatus.Called)
                {
                    call.MarkSkipped(now);
                }

                break;
        }
    }

    private async Task<WaitingRoomCardDto> BuildCardAsync(
        WaitingRoomCall call,
        CancellationToken cancellationToken)
    {
        var appointment = await _appointmentStore.GetWithRelationsAsync(call.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        var doctor = await _doctorStore.GetTrackedWithRelationsAsync(call.DoctorId, cancellationToken);
        var roomName = call.RoomId.HasValue
            ? (await _roomStore.GetTrackedByIdAsync(call.RoomId.Value, cancellationToken))?.Name
            : null;

        return new WaitingRoomCardDto(
            call.Id,
            call.AppointmentId,
            call.SequenceNumber,
            appointment.PatientId,
            appointment.Patient.FullName(),
            call.DoctorId,
            doctor is null ? string.Empty : $"{doctor.FirstName} {doctor.LastName}",
            call.ClinicId,
            appointment.Clinic?.Name ?? string.Empty,
            call.RoomId,
            roomName,
            call.LocalDate,
            call.Status,
            call.RecallCount,
            call.CalledAt,
            call.AnsweredAt,
            call.SkippedAt,
            call.CompletedAt,
            appointment.StartAt,
            call.CreatedAt,
            call.Note);
    }

    private void EnqueueAudit(Guid? userId, string action, Guid entityId, DateTimeOffset now, object? newValues)
    {
        _auditWriter.Enqueue(new AuditEntry(
            userId, action, nameof(WaitingRoomCall), entityId, now,
            _requestMetadata.IpAddress, _requestMetadata.UserAgent, null,
            newValues is null ? null : System.Text.Json.JsonSerializer.Serialize(newValues, JsonDefaults.Options),
            _correlationIdProvider.CorrelationId));
    }

    private static string QueueLockResource(Guid doctorId, Guid clinicId, DateOnly localDate)
    {
        return $"waitingroom:doctor:{doctorId}:clinic:{clinicId}:date:{localDate:yyyy-MM-dd}";
    }
}