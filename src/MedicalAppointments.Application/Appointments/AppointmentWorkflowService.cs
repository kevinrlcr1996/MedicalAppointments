using MedicalAppointments.Application.Appointments.Auth;
using MedicalAppointments.Application.Appointments.Outbox;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Audit;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.MedicalStructure.Patients;
using MedicalAppointments.Application.WaitingList.Outbox;
using MedicalAppointments.Domain.Appointments;

namespace MedicalAppointments.Application.Appointments;

public sealed class AppointmentWorkflowService : IAppointmentWorkflowService
{
    private readonly IAppointmentStore _store;
    private readonly IAppointmentAccessService _accessService;
    private readonly IPatientStore _patientStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppointmentEventNotifier _eventNotifier;
    private readonly IAuditWriter _auditWriter;
    private readonly IRequestMetadataProvider _requestMetadata;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly IWaitingListEventNotifier _waitingListNotifier;
    private readonly TimeProvider _timeProvider;

    public AppointmentWorkflowService(
        IAppointmentStore store,
        IAppointmentAccessService accessService,
        IPatientStore patientStore,
        IUnitOfWork unitOfWork,
        IAppointmentEventNotifier eventNotifier,
        IAuditWriter auditWriter,
        IRequestMetadataProvider requestMetadata,
        ICorrelationIdProvider correlationIdProvider,
        IWaitingListEventNotifier waitingListNotifier,
        TimeProvider timeProvider)
    {
        _store = store;
        _accessService = accessService;
        _patientStore = patientStore;
        _unitOfWork = unitOfWork;
        _eventNotifier = eventNotifier;
        _auditWriter = auditWriter;
        _requestMetadata = requestMetadata;
        _correlationIdProvider = correlationIdProvider;
        _waitingListNotifier = waitingListNotifier;
        _timeProvider = timeProvider;
    }

    public Task<AppointmentDto> ConfirmAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return TransitionAsync(
            id,
            AppointmentStatus.Confirmed,
            "Appointment.Confirmed",
            (appointment, now) => appointment.Confirm(now),
            (appointment, subject) => _accessService.CanConfirm(appointment, subject),
            cancellationToken);
    }

    public Task<AppointmentDto> CheckInAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return TransitionAsync(
            id,
            AppointmentStatus.CheckedIn,
            "Appointment.CheckedIn",
            (appointment, now) => appointment.CheckIn(now),
            (appointment, subject) => _accessService.CanCheckIn(appointment, subject),
            cancellationToken);
    }

    public Task<AppointmentDto> StartAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return TransitionAsync(
            id,
            AppointmentStatus.InProgress,
            "Appointment.Started",
            (appointment, now) => appointment.Start(now),
            (appointment, subject) => _accessService.CanStart(appointment, subject),
            cancellationToken);
    }

    public Task<AppointmentDto> CompleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return TransitionAsync(
            id,
            AppointmentStatus.Completed,
            "Appointment.Completed",
            (appointment, now) => appointment.Complete(now),
            (appointment, subject) => _accessService.CanComplete(appointment, subject),
            cancellationToken);
    }

    public Task<AppointmentDto> MarkNoShowAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return TransitionAsync(
            id,
            AppointmentStatus.NoShow,
            "Appointment.NoShow",
            (appointment, now) => appointment.MarkNoShow(now),
            (appointment, subject) => _accessService.CanMarkNoShow(appointment, subject),
            cancellationToken);
    }

    private async Task<AppointmentDto> TransitionAsync(
        Guid id,
        AppointmentStatus targetStatus,
        string auditAction,
        Action<Appointment, DateTimeOffset> transition,
        Func<Appointment, AppointmentAccessSubject, bool> authorization,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var subject = await _accessService.GetSubjectAsync(cancellationToken);

        var appointment = await _store.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        if (!authorization(appointment, subject))
        {
            throw new ForbiddenException("You do not have permission for this appointment action.");
        }

        if (appointment.Status == targetStatus)
        {
            return await GetDtoAsync(id, cancellationToken);
        }

        var oldStatus = appointment.Status;
        var patientUserId = await _patientStore.GetUserIdAsync(appointment.PatientId, cancellationToken);

        transition(appointment, now);

        _eventNotifier.EnqueueStatusChanged(
            appointment,
            patientUserId,
            oldStatus,
            targetStatus,
            now,
            _correlationIdProvider.CorrelationId);

        EnqueueSlotFreedIfFuture(appointment, now);

        _auditWriter.Enqueue(new AuditEntry(
            UserId: subject.UserId,
            Action: auditAction,
            EntityName: nameof(Appointment),
            EntityId: appointment.Id,
            Timestamp: now,
            IpAddress: _requestMetadata.IpAddress,
            UserAgent: _requestMetadata.UserAgent,
            OldValues: System.Text.Json.JsonSerializer.Serialize(new { status = oldStatus.ToString() }, JsonDefaults.Options),
            NewValues: System.Text.Json.JsonSerializer.Serialize(new { status = targetStatus.ToString() }, JsonDefaults.Options),
            CorrelationId: _correlationIdProvider.CorrelationId));

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await GetDtoAsync(id, cancellationToken);
    }

    private async Task<AppointmentDto> GetDtoAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _store.GetWithRelationsAsync(id, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        return AppointmentQueryService.Map(entity);
    }

    private void EnqueueSlotFreedIfFuture(
    Domain.Appointments.Appointment appointment,
    DateTimeOffset now)
    {
        // Solo los estados que liberan un slot FUTURO re-alimentan la waiting list.
        if (appointment.StartAt <= now)
        {
            return;
        }

        if (appointment.Status
            is not (Domain.Appointments.AppointmentStatus.Cancelled
                or Domain.Appointments.AppointmentStatus.NoShow
                or Domain.Appointments.AppointmentStatus.Rescheduled))
        {
            return;
        }

        _waitingListNotifier.EnqueueSlotFreed(
            appointment.DoctorId,
            appointment.ClinicId,
            appointment.RoomId,
            appointment.LocalDate,
            appointment.StartAt,
            appointment.EndAt,
            now,
            _correlationIdProvider.CorrelationId);
    }
}