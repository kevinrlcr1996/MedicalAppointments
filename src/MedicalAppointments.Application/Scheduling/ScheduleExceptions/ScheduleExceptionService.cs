using FluentValidation;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.MedicalStructure.Clinics;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Application.Scheduling.Outbox;
using MedicalAppointments.Domain.Scheduling;

namespace MedicalAppointments.Application.Scheduling.ScheduleExceptions;

public sealed class ScheduleExceptionService : IScheduleExceptionService
{
    private readonly IScheduleExceptionStore _store;
    private readonly IDoctorStore _doctorStore;
    private readonly IClinicStore _clinicStore;
    private readonly ISchedulingEventNotifier _notifier;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<CreateScheduleExceptionCommand> _createValidator;
    private readonly IValidator<UpdateScheduleExceptionCommand> _updateValidator;

    public ScheduleExceptionService(
        IScheduleExceptionStore store,
        IDoctorStore doctorStore,
        IClinicStore clinicStore,
        ISchedulingEventNotifier notifier,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IValidator<CreateScheduleExceptionCommand> createValidator,
        IValidator<UpdateScheduleExceptionCommand> updateValidator)
    {
        _store = store;
        _doctorStore = doctorStore;
        _clinicStore = clinicStore;
        _notifier = notifier;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<ScheduleExceptionDto>> GetPagedAsync(
        ScheduleExceptionQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _store.GetPagedAsync(query, cancellationToken);

        return new PagedResult<ScheduleExceptionDto>(
            result.Items.Select(Map).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
    }

    public async Task<ScheduleExceptionDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _store.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Schedule exception not found.");

        return Map(entity);
    }

    public async Task<ScheduleExceptionDto> CreateAsync(
        CreateScheduleExceptionCommand command,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(command, cancellationToken);

        if (!await _doctorStore.ExistsAsync(command.DoctorId, cancellationToken))
        {
            throw new NotFoundException("Doctor not found.");
        }

        if (!await _clinicStore.ExistsAsync([command.ClinicId], cancellationToken))
        {
            throw new NotFoundException("Clinic not found.");
        }

        var now = _timeProvider.GetUtcNow();

        var entity = ScheduleException.Create(
            command.DoctorId,
            command.ClinicId,
            command.StartDate,
            command.EndDate,
            command.StartTime,
            command.EndTime,
            command.Type,
            command.Reason,
            command.IsActive,
            now);

        await _store.AddAsync(entity, cancellationToken);

        _notifier.EnqueueAvailabilityChanged(
            entity.DoctorId,
            entity.ClinicId,
            entity.StartDate,
            entity.EndDate);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<ScheduleExceptionDto> UpdateAsync(
        UpdateScheduleExceptionCommand command,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();

        var entity = await _store.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Schedule exception not found.");

        entity.Update(
            command.StartDate,
            command.EndDate,
            command.StartTime,
            command.EndTime,
            command.Type,
            command.Reason,
            command.IsActive,
            now);

        _notifier.EnqueueAvailabilityChanged(
            entity.DoctorId,
            entity.ClinicId,
            entity.StartDate,
            entity.EndDate);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<ScheduleExceptionDto> SetActiveAsync(
        SetScheduleExceptionActiveCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        var entity = await _store.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Schedule exception not found.");

        entity.SetActive(command.IsActive, now);

        _notifier.EnqueueAvailabilityChanged(
            entity.DoctorId,
            entity.ClinicId,
            entity.StartDate,
            entity.EndDate);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    private static ScheduleExceptionDto Map(ScheduleException entity)
    {
        return new ScheduleExceptionDto(
            entity.Id,
            entity.DoctorId,
            entity.ClinicId,
            entity.StartDate,
            entity.EndDate,
            entity.StartTime,
            entity.EndTime,
            entity.Type,
            entity.Reason,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}