using FluentValidation;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Application.MedicalStructure.Specialties;

public sealed class SpecialtyService : ISpecialtyService
{
    private readonly ISpecialtyStore _store;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<CreateSpecialtyCommand> _createValidator;
    private readonly IValidator<UpdateSpecialtyCommand> _updateValidator;

    public SpecialtyService(
        ISpecialtyStore store,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IValidator<CreateSpecialtyCommand> createValidator,
        IValidator<UpdateSpecialtyCommand> updateValidator)
    {
        _store = store;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<SpecialtyDto>> GetPagedAsync(
        SpecialtyQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _store.GetPagedAsync(query, cancellationToken);

        return new PagedResult<SpecialtyDto>(
            result.Items.Select(Map).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
    }

    public async Task<SpecialtyDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _store.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Specialty not found.");

        return Map(entity);
    }

    public async Task<SpecialtyDto> CreateAsync(
        CreateSpecialtyCommand command,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        var normalizedName = StringNormalizer.NormalizeUpper(command.Name);
        var normalizedCode = StringNormalizer.NormalizeUpperOrNull(command.Code);

        if (await _store.ExistsNormalizedNameAsync(normalizedName, null, cancellationToken))
        {
            throw new ConflictException("A specialty with this name already exists.");
        }

        if (normalizedCode is not null &&
            await _store.ExistsNormalizedCodeAsync(normalizedCode, null, cancellationToken))
        {
            throw new ConflictException("A specialty with this code already exists.");
        }

        var entity = Specialty.Create(
            command.Name,
            command.Code,
            command.Description,
            command.IsActive,
            now);

        await _store.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<SpecialtyDto> UpdateAsync(
        UpdateSpecialtyCommand command,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        var entity = await _store.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Specialty not found.");

        var normalizedName = StringNormalizer.NormalizeUpper(command.Name);
        var normalizedCode = StringNormalizer.NormalizeUpperOrNull(command.Code);

        if (await _store.ExistsNormalizedNameAsync(normalizedName, command.Id, cancellationToken))
        {
            throw new ConflictException("A specialty with this name already exists.");
        }

        if (normalizedCode is not null &&
            await _store.ExistsNormalizedCodeAsync(normalizedCode, command.Id, cancellationToken))
        {
            throw new ConflictException("A specialty with this code already exists.");
        }

        entity.Update(
            command.Name,
            command.Code,
            command.Description,
            command.IsActive,
            now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<SpecialtyDto> SetActiveAsync(
        SetSpecialtyActiveCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var entity = await _store.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Specialty not found.");

        entity.SetActive(command.IsActive, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    private static SpecialtyDto Map(Specialty entity)
    {
        return new SpecialtyDto(
            entity.Id,
            entity.Name,
            entity.Code,
            entity.Description,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}