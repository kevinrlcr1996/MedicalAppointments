using FluentValidation;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.MedicalStructure.Clinics;
using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Application.MedicalStructure.Resources;

public sealed class ResourceService : IResourceService
{
    private readonly IResourceStore _store;
    private readonly IClinicStore _clinicStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<CreateResourceCommand> _createValidator;
    private readonly IValidator<UpdateResourceCommand> _updateValidator;

    public ResourceService(
        IResourceStore store,
        IClinicStore clinicStore,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IValidator<CreateResourceCommand> createValidator,
        IValidator<UpdateResourceCommand> updateValidator)
    {
        _store = store;
        _clinicStore = clinicStore;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<ResourceDto>> GetPagedAsync(
        ResourceQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _store.GetPagedAsync(query, cancellationToken);

        return new PagedResult<ResourceDto>(
            result.Items.Select(Map).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
    }

    public async Task<ResourceDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _store.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Resource not found.");

        return Map(entity);
    }

    public async Task<ResourceDto> CreateAsync(
        CreateResourceCommand command,
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
            throw new ConflictException("A resource with this name already exists in this clinic.");
        }

        if (normalizedCode is not null &&
            await _store.ExistsNormalizedCodeInClinicAsync(
                command.ClinicId,
                normalizedCode,
                null,
                cancellationToken))
        {
            throw new ConflictException("A resource with this code already exists in this clinic.");
        }

        var entity = Resource.Create(
            command.ClinicId,
            command.Name,
            command.Code,
            command.Kind,
            command.Description,
            command.IsActive,
            now);

        await _store.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<ResourceDto> UpdateAsync(
        UpdateResourceCommand command,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        var entity = await _store.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Resource not found.");

        var normalizedName = StringNormalizer.NormalizeUpper(command.Name);
        var normalizedCode = StringNormalizer.NormalizeUpperOrNull(command.Code);

        if (await _store.ExistsNormalizedNameInClinicAsync(
                entity.ClinicId,
                normalizedName,
                command.Id,
                cancellationToken))
        {
            throw new ConflictException("A resource with this name already exists in this clinic.");
        }

        if (normalizedCode is not null &&
            await _store.ExistsNormalizedCodeInClinicAsync(
                entity.ClinicId,
                normalizedCode,
                command.Id,
                cancellationToken))
        {
            throw new ConflictException("A resource with this code already exists in this clinic.");
        }

        entity.Update(
            command.Name,
            command.Code,
            command.Kind,
            command.Description,
            command.IsActive,
            now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<ResourceDto> SetActiveAsync(
        SetResourceActiveCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var entity = await _store.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Resource not found.");

        entity.SetActive(command.IsActive, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    private static ResourceDto Map(Resource entity)
    {
        return new ResourceDto(
            entity.Id,
            entity.ClinicId,
            entity.Clinic?.Name ?? string.Empty,
            entity.Name,
            entity.Code,
            entity.Kind,
            entity.Description,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}