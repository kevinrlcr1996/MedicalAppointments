using FluentValidation;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.MedicalStructure.Clinics;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Application.Scheduling.Outbox;
using MedicalAppointments.Domain.Scheduling;

namespace MedicalAppointments.Application.Scheduling.Blockouts;

public sealed class BlockoutService : IBlockoutService
{
    private readonly IBlockoutStore _store;
    private readonly IDoctorStore _doctorStore;
    private readonly IClinicStore _clinicStore;
    private readonly ISchedulingEventNotifier _notifier;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<CreateBlockoutCommand> _createValidator;
    private readonly IValidator<UpdateBlockoutCommand> _updateValidator;

    public BlockoutService(
        IBlockoutStore store,
        IDoctorStore doctorStore,
        IClinicStore clinicStore,
        ISchedulingEventNotifier notifier,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IValidator<CreateBlockoutCommand> createValidator,
        IValidator<UpdateBlockoutCommand> updateValidator)
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

    public async Task<PagedResult<BlockoutDto>> GetPagedAsync(
        BlockoutQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _store.GetPagedAsync(query, cancellationToken);

        return new PagedResult<BlockoutDto>(
            result.Items.Select(Map).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
    }

    public async Task<BlockoutDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _store.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Blockout not found.");

        return Map(entity);
    }

    public async Task<BlockoutDto> CreateAsync(
        CreateBlockoutCommand command,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(command, cancellationToken);

        if (command.DoctorId.HasValue &&
            !await _doctorStore.ExistsAsync(command.DoctorId.Value, cancellationToken))
        {
            throw new NotFoundException("Doctor not found.");
        }

        if (command.ClinicId.HasValue &&
            !await _clinicStore.ExistsAsync([command.ClinicId.Value], cancellationToken))
        {
            throw new NotFoundException("Clinic not found.");
        }

        var now = _timeProvider.GetUtcNow();

        var entity = Blockout.Create(
            command.DoctorId,
            command.ClinicId,
            command.RoomId,
            command.ResourceId,
            command.StartAt,
            command.EndAt,
            command.Reason,
            command.IsActive,
            now);

        await _store.AddAsync(entity, cancellationToken);

        if (entity.DoctorId.HasValue && entity.ClinicId.HasValue)
        {
            _notifier.EnqueueAvailabilityChanged(
                entity.DoctorId.Value,
                entity.ClinicId.Value,
                DateOnly.FromDateTime(entity.StartAt.UtcDateTime),
                DateOnly.FromDateTime(entity.EndAt.UtcDateTime));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<BlockoutDto> UpdateAsync(
        UpdateBlockoutCommand command,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();

        var entity = await _store.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Blockout not found.");

        if (command.DoctorId.HasValue &&
            !await _doctorStore.ExistsAsync(command.DoctorId.Value, cancellationToken))
        {
            throw new NotFoundException("Doctor not found.");
        }

        if (command.ClinicId.HasValue &&
            !await _clinicStore.ExistsAsync([command.ClinicId.Value], cancellationToken))
        {
            throw new NotFoundException("Clinic not found.");
        }

        entity.Update(
            command.DoctorId,
            command.ClinicId,
            command.RoomId,
            command.ResourceId,
            command.StartAt,
            command.EndAt,
            command.Reason,
            command.IsActive,
            now);

        if (entity.DoctorId.HasValue && entity.ClinicId.HasValue)
        {
            _notifier.EnqueueAvailabilityChanged(
                entity.DoctorId.Value,
                entity.ClinicId.Value,
                DateOnly.FromDateTime(entity.StartAt.UtcDateTime),
                DateOnly.FromDateTime(entity.EndAt.UtcDateTime));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<BlockoutDto> SetActiveAsync(
        SetBlockoutActiveCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        var entity = await _store.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Blockout not found.");

        entity.SetActive(command.IsActive, now);

        if (entity.DoctorId.HasValue && entity.ClinicId.HasValue)
        {
            _notifier.EnqueueAvailabilityChanged(
                entity.DoctorId.Value,
                entity.ClinicId.Value,
                DateOnly.FromDateTime(entity.StartAt.UtcDateTime),
                DateOnly.FromDateTime(entity.EndAt.UtcDateTime));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    private static BlockoutDto Map(Blockout entity)
    {
        return new BlockoutDto(
            entity.Id,
            entity.DoctorId,
            entity.ClinicId,
            entity.RoomId,
            entity.ResourceId,
            entity.StartAt,
            entity.EndAt,
            entity.Reason,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}