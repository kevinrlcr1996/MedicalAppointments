using FluentValidation;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.MedicalStructure.Clinics;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Application.Scheduling.Outbox;
using MedicalAppointments.Domain.MedicalStructure;
using MedicalAppointments.Domain.Scheduling;

namespace MedicalAppointments.Application.Scheduling.DoctorSchedules;

public sealed class DoctorScheduleService : IDoctorScheduleService
{
    private readonly IDoctorScheduleStore _store;
    private readonly IDoctorStore _doctorStore;
    private readonly IClinicStore _clinicStore;
    private readonly ISchedulingEventNotifier _notifier;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<CreateDoctorScheduleCommand> _createValidator;
    private readonly IValidator<UpdateDoctorScheduleCommand> _updateValidator;

    public DoctorScheduleService(
        IDoctorScheduleStore store,
        IDoctorStore doctorStore,
        IClinicStore clinicStore,
        ISchedulingEventNotifier notifier,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IValidator<CreateDoctorScheduleCommand> createValidator,
        IValidator<UpdateDoctorScheduleCommand> updateValidator)
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

    public async Task<PagedResult<ScheduleDto>> GetPagedAsync(
        DoctorScheduleQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _store.GetPagedAsync(query, cancellationToken);

        return new PagedResult<ScheduleDto>(
            result.Items.Select(Map).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
    }

    public async Task<ScheduleDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _store.GetTrackedWithDaysAsync(id, cancellationToken)
            ?? throw new NotFoundException("Doctor schedule not found.");

        return Map(entity);
    }

    public async Task<ScheduleDto> CreateAsync(
        CreateDoctorScheduleCommand command,
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

        var entity = DoctorSchedule.Create(
            command.DoctorId,
            command.ClinicId,
            command.Name,
            command.IsDefault,
            command.IsActive,
            command.EffectiveFrom,
            command.EffectiveTo,
            command.SlotDurationMinutes,
            command.BufferMinutes,
            command.Days.Select(x => new DoctorScheduleDayInput(x.DayOfWeek, x.StartTime, x.EndTime)),
            now);

        await _store.AddAsync(entity, cancellationToken);

        _notifier.EnqueueScheduleChanged(
            entity.Id,
            entity.DoctorId,
            entity.ClinicId,
            "Created",
            entity.EffectiveFrom,
            entity.EffectiveTo ?? entity.EffectiveFrom);

        _notifier.EnqueueAvailabilityChanged(
            entity.DoctorId,
            entity.ClinicId,
            entity.EffectiveFrom,
            entity.EffectiveTo ?? entity.EffectiveFrom);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<ScheduleDto> UpdateAsync(
        UpdateDoctorScheduleCommand command,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();

        var entity = await _store.GetTrackedWithDaysAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Doctor schedule not found.");

        entity.Update(
            command.Name,
            command.IsDefault,
            command.IsActive,
            command.EffectiveFrom,
            command.EffectiveTo,
            command.SlotDurationMinutes,
            command.BufferMinutes,
            command.Days.Select(x => new DoctorScheduleDayInput(x.DayOfWeek, x.StartTime, x.EndTime)),
            now);

        _notifier.EnqueueScheduleChanged(
            entity.Id,
            entity.DoctorId,
            entity.ClinicId,
            "Updated",
            entity.EffectiveFrom,
            entity.EffectiveTo ?? entity.EffectiveFrom);

        _notifier.EnqueueAvailabilityChanged(
            entity.DoctorId,
            entity.ClinicId,
            entity.EffectiveFrom,
            entity.EffectiveTo ?? entity.EffectiveFrom);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<ScheduleDto> SetActiveAsync(
        SetDoctorScheduleActiveCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        var entity = await _store.GetTrackedWithDaysAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Doctor schedule not found.");

        entity.SetActive(command.IsActive, now);

        _notifier.EnqueueScheduleChanged(
            entity.Id,
            entity.DoctorId,
            entity.ClinicId,
            command.IsActive ? "Activated" : "Deactivated",
            entity.EffectiveFrom,
            entity.EffectiveTo ?? entity.EffectiveFrom);

        _notifier.EnqueueAvailabilityChanged(
            entity.DoctorId,
            entity.ClinicId,
            entity.EffectiveFrom,
            entity.EffectiveTo ?? entity.EffectiveFrom);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    private static ScheduleDto Map(DoctorSchedule entity)
    {
        return new ScheduleDto(
            entity.Id,
            entity.DoctorId,
            entity.ClinicId,
            entity.Name,
            entity.IsDefault,
            entity.IsActive,
            entity.EffectiveFrom,
            entity.EffectiveTo,
            entity.SlotDurationMinutes,
            entity.BufferMinutes,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.Days
                .OrderBy(x => x.DayOfWeek)
                .Select(x => new ScheduleDayDto(x.DayOfWeek, x.StartTime, x.EndTime))
                .ToList());
    }
}