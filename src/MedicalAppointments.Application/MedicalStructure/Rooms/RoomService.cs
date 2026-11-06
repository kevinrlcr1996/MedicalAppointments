using FluentValidation;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.MedicalStructure.Clinics;
using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Application.MedicalStructure.Rooms;

public sealed class RoomService : IRoomService
{
    private readonly IRoomStore _store;
    private readonly IClinicStore _clinicStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<CreateRoomCommand> _createValidator;
    private readonly IValidator<UpdateRoomCommand> _updateValidator;

    public RoomService(
        IRoomStore store,
        IClinicStore clinicStore,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IValidator<CreateRoomCommand> createValidator,
        IValidator<UpdateRoomCommand> updateValidator)
    {
        _store = store;
        _clinicStore = clinicStore;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<RoomDto>> GetPagedAsync(
        RoomQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _store.GetPagedAsync(query, cancellationToken);

        return new PagedResult<RoomDto>(
            result.Items.Select(Map).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
    }

    public async Task<RoomDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _store.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Room not found.");

        return Map(entity);
    }

    public async Task<RoomDto> CreateAsync(
        CreateRoomCommand command,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(command, cancellationToken);

        if (!await _clinicStore.ExistsAsync([command.ClinicId], cancellationToken))
        {
            throw new NotFoundException("Clinic not found.");
        }

        var now = _timeProvider.GetUtcNow();
        var normalizedName = StringNormalizer.NormalizeUpper(command.Name);
        var normalizedCode = StringNormalizer.NormalizeUpperOrNull(command.Code);

        if (await _store.ExistsNormalizedNameInClinicAsync(
                command.ClinicId,
                normalizedName,
                null,
                cancellationToken))
        {
            throw new ConflictException("A room with this name already exists in this clinic.");
        }

        if (normalizedCode is not null &&
            await _store.ExistsNormalizedCodeInClinicAsync(
                command.ClinicId,
                normalizedCode,
                null,
                cancellationToken))
        {
            throw new ConflictException("A room with this code already exists in this clinic.");
        }

        var entity = Room.Create(
            command.ClinicId,
            command.Name,
            command.Code,
            command.Capacity,
            command.Notes,
            command.IsActive,
            now);

        await _store.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<RoomDto> UpdateAsync(
        UpdateRoomCommand command,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        var entity = await _store.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Room not found.");

        var normalizedName = StringNormalizer.NormalizeUpper(command.Name);
        var normalizedCode = StringNormalizer.NormalizeUpperOrNull(command.Code);

        if (await _store.ExistsNormalizedNameInClinicAsync(
                entity.ClinicId,
                normalizedName,
                command.Id,
                cancellationToken))
        {
            throw new ConflictException("A room with this name already exists in this clinic.");
        }

        if (normalizedCode is not null &&
            await _store.ExistsNormalizedCodeInClinicAsync(
                entity.ClinicId,
                normalizedCode,
                command.Id,
                cancellationToken))
        {
            throw new ConflictException("A room with this code already exists in this clinic.");
        }

        entity.Update(
            command.Name,
            command.Code,
            command.Capacity,
            command.Notes,
            command.IsActive,
            now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<RoomDto> SetActiveAsync(
        SetRoomActiveCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var entity = await _store.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Room not found.");

        entity.SetActive(command.IsActive, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    private static RoomDto Map(Room entity)
    {
        return new RoomDto(
            entity.Id,
            entity.ClinicId,
            entity.Clinic?.Name ?? string.Empty,
            entity.Name,
            entity.Code,
            entity.Capacity,
            entity.Notes,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}