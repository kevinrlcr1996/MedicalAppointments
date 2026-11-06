using FluentValidation;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.MedicalStructure.Organizations;
using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Application.MedicalStructure.Clinics;

public sealed class ClinicService : IClinicService
{
    private readonly IClinicStore _store;
    private readonly IOrganizationStore _organizationStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<CreateClinicCommand> _createValidator;
    private readonly IValidator<UpdateClinicCommand> _updateValidator;

    public ClinicService(
        IClinicStore store,
        IOrganizationStore organizationStore,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IValidator<CreateClinicCommand> createValidator,
        IValidator<UpdateClinicCommand> updateValidator)
    {
        _store = store;
        _organizationStore = organizationStore;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<ClinicDto>> GetPagedAsync(
        ClinicQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _store.GetPagedAsync(query, cancellationToken);

        return new PagedResult<ClinicDto>(
            result.Items.Select(Map).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
    }

    public async Task<ClinicDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _store.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Clinic not found.");

        return Map(entity);
    }

    public async Task<ClinicDto> CreateAsync(
        CreateClinicCommand command,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(command, cancellationToken);

        if (!await _organizationStore.ExistsAsync([command.OrganizationId], cancellationToken))
        {
            throw new NotFoundException("Organization not found.");
        }

        var now = _timeProvider.GetUtcNow();
        var normalizedName = StringNormalizer.NormalizeUpper(command.Name);
        var normalizedCode = StringNormalizer.NormalizeUpperOrNull(command.Code);

        if (await _store.ExistsNormalizedNameInOrganizationAsync(
                command.OrganizationId,
                normalizedName,
                null,
                cancellationToken))
        {
            throw new ConflictException("A clinic with this name already exists in this organization.");
        }

        if (normalizedCode is not null &&
            await _store.ExistsNormalizedCodeInOrganizationAsync(
                command.OrganizationId,
                normalizedCode,
                null,
                cancellationToken))
        {
            throw new ConflictException("A clinic with this code already exists in this organization.");
        }

        var entity = Clinic.Create(
            command.OrganizationId,
            command.Name,
            command.Code,
            command.Description,
            command.Address,
            command.City,
            command.Country,
            command.Phone,
            command.Email,
            command.TimeZoneId,
            command.IsActive,
            now);

        await _store.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<ClinicDto> UpdateAsync(
        UpdateClinicCommand command,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        var entity = await _store.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Clinic not found.");

        var normalizedName = StringNormalizer.NormalizeUpper(command.Name);
        var normalizedCode = StringNormalizer.NormalizeUpperOrNull(command.Code);

        if (await _store.ExistsNormalizedNameInOrganizationAsync(
                entity.OrganizationId,
                normalizedName,
                command.Id,
                cancellationToken))
        {
            throw new ConflictException("A clinic with this name already exists in this organization.");
        }

        if (normalizedCode is not null &&
            await _store.ExistsNormalizedCodeInOrganizationAsync(
                entity.OrganizationId,
                normalizedCode,
                command.Id,
                cancellationToken))
        {
            throw new ConflictException("A clinic with this code already exists in this organization.");
        }

        entity.Update(
            command.Name,
            command.Code,
            command.Description,
            command.Address,
            command.City,
            command.Country,
            command.Phone,
            command.Email,
            command.TimeZoneId,
            command.IsActive,
            now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<ClinicDto> SetActiveAsync(
        SetClinicActiveCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var entity = await _store.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Clinic not found.");

        entity.SetActive(command.IsActive, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    private static ClinicDto Map(Clinic entity)
    {
        return new ClinicDto(
            entity.Id,
            entity.OrganizationId,
            entity.Organization?.Name ?? string.Empty,
            entity.Name,
            entity.Code,
            entity.Description,
            entity.Address,
            entity.City,
            entity.Country,
            entity.Phone,
            entity.Email,
            entity.TimeZoneId,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}