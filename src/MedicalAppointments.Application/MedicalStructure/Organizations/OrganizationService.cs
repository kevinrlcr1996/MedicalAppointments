using FluentValidation;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Application.MedicalStructure.Organizations;

public sealed class OrganizationService : IOrganizationService
{
    private readonly IOrganizationStore _store;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<CreateOrganizationCommand> _createValidator;
    private readonly IValidator<UpdateOrganizationCommand> _updateValidator;

    public OrganizationService(
        IOrganizationStore store,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IValidator<CreateOrganizationCommand> createValidator,
        IValidator<UpdateOrganizationCommand> updateValidator)
    {
        _store = store;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<OrganizationDto>> GetPagedAsync(
        OrganizationQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _store.GetPagedAsync(query, cancellationToken);

        return new PagedResult<OrganizationDto>(
            result.Items.Select(Map).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
    }

    public async Task<OrganizationDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _store.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Organization not found.");

        return Map(entity);
    }

    public async Task<OrganizationDto> CreateAsync(
        CreateOrganizationCommand command,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        var normalizedName = StringNormalizer.NormalizeUpper(command.Name);
        var normalizedTaxId = StringNormalizer.NormalizeUpperOrNull(command.TaxId);

        if (await _store.ExistsNormalizedNameAsync(normalizedName, null, cancellationToken))
        {
            throw new ConflictException("An organization with this name already exists.");
        }

        if (normalizedTaxId is not null &&
            await _store.ExistsNormalizedTaxIdAsync(normalizedTaxId, null, cancellationToken))
        {
            throw new ConflictException("An organization with this tax id already exists.");
        }

        var entity = Organization.Create(
            command.Name,
            command.LegalName,
            command.TaxId,
            command.Email,
            command.Phone,
            command.Address,
            command.City,
            command.Country,
            command.IsActive,
            now);

        await _store.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<OrganizationDto> UpdateAsync(
        UpdateOrganizationCommand command,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        var entity = await _store.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Organization not found.");

        var normalizedName = StringNormalizer.NormalizeUpper(command.Name);
        var normalizedTaxId = StringNormalizer.NormalizeUpperOrNull(command.TaxId);

        if (await _store.ExistsNormalizedNameAsync(normalizedName, command.Id, cancellationToken))
        {
            throw new ConflictException("An organization with this name already exists.");
        }

        if (normalizedTaxId is not null &&
            await _store.ExistsNormalizedTaxIdAsync(normalizedTaxId, command.Id, cancellationToken))
        {
            throw new ConflictException("An organization with this tax id already exists.");
        }

        entity.Update(
            command.Name,
            command.LegalName,
            command.TaxId,
            command.Email,
            command.Phone,
            command.Address,
            command.City,
            command.Country,
            command.IsActive,
            now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<OrganizationDto> SetActiveAsync(
        SetOrganizationActiveCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var entity = await _store.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Organization not found.");

        entity.SetActive(command.IsActive, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    private static OrganizationDto Map(Organization entity)
    {
        return new OrganizationDto(
            entity.Id,
            entity.Name,
            entity.LegalName,
            entity.TaxId,
            entity.Email,
            entity.Phone,
            entity.Address,
            entity.City,
            entity.Country,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}