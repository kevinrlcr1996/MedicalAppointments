using FluentValidation;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.MedicalStructure.Clinics;
using MedicalAppointments.Application.MedicalStructure.Organizations;
using MedicalAppointments.Application.Scheduling.Outbox;
using MedicalAppointments.Domain.Scheduling;

namespace MedicalAppointments.Application.Scheduling.Holidays;

public sealed class HolidayService : IHolidayService
{
    private readonly IHolidayStore _store;
    private readonly IOrganizationStore _organizationStore;
    private readonly IClinicStore _clinicStore;
    private readonly ISchedulingEventNotifier _notifier;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<CreateHolidayCommand> _createValidator;
    private readonly IValidator<UpdateHolidayCommand> _updateValidator;

    public HolidayService(
        IHolidayStore store,
        IOrganizationStore organizationStore,
        IClinicStore clinicStore,
        ISchedulingEventNotifier notifier,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IValidator<CreateHolidayCommand> createValidator,
        IValidator<UpdateHolidayCommand> updateValidator)
    {
        _store = store;
        _organizationStore = organizationStore;
        _clinicStore = clinicStore;
        _notifier = notifier;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<HolidayDto>> GetPagedAsync(
        HolidayQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _store.GetPagedAsync(query, cancellationToken);

        return new PagedResult<HolidayDto>(
            result.Items.Select(Map).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
    }

    public async Task<HolidayDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _store.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Holiday not found.");

        return Map(entity);
    }

    public async Task<HolidayDto> CreateAsync(
        CreateHolidayCommand command,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(command, cancellationToken);

        if (command.OrganizationId.HasValue &&
            !await _organizationStore.ExistsAsync([command.OrganizationId.Value], cancellationToken))
        {
            throw new NotFoundException("Organization not found.");
        }

        if (command.ClinicId.HasValue &&
            !await _clinicStore.ExistsAsync([command.ClinicId.Value], cancellationToken))
        {
            throw new NotFoundException("Clinic not found.");
        }

        var now = _timeProvider.GetUtcNow();

        var entity = Holiday.Create(
            command.OrganizationId,
            command.ClinicId,
            command.Date,
            command.Name,
            command.Description,
            command.IsActive,
            now);

        await _store.AddAsync(entity, cancellationToken);

        await NotifyAffectedAvailabilityAsync(entity, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<HolidayDto> UpdateAsync(
        UpdateHolidayCommand command,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();

        var entity = await _store.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Holiday not found.");

        if (command.OrganizationId.HasValue &&
            !await _organizationStore.ExistsAsync([command.OrganizationId.Value], cancellationToken))
        {
            throw new NotFoundException("Organization not found.");
        }

        if (command.ClinicId.HasValue &&
            !await _clinicStore.ExistsAsync([command.ClinicId.Value], cancellationToken))
        {
            throw new NotFoundException("Clinic not found.");
        }

        entity.Update(
            command.OrganizationId,
            command.ClinicId,
            command.Date,
            command.Name,
            command.Description,
            command.IsActive,
            now);

        await NotifyAffectedAvailabilityAsync(entity, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<HolidayDto> SetActiveAsync(
        SetHolidayActiveCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        var entity = await _store.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Holiday not found.");

        entity.SetActive(command.IsActive, now);

        await NotifyAffectedAvailabilityAsync(entity, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    private async Task NotifyAffectedAvailabilityAsync(
        Holiday entity,
        CancellationToken cancellationToken)
    {
        if (entity.ClinicId.HasValue)
        {
            var clinic = await _clinicStore.GetTrackedByIdAsync(entity.ClinicId.Value, cancellationToken);

            if (clinic is not null)
            {
                var doctors = Array.Empty<Guid>();

                foreach (var doctorId in doctors)
                {
                    _notifier.EnqueueAvailabilityChanged(
                        doctorId,
                        clinic.Id,
                        entity.Date,
                        entity.Date);
                }
            }

            return;
        }

        // Para festivos globales u organizacionales, la invalidación exacta por médico
        // se resolverá cuando exista índice doctor-clinic más rico o eventos por organización.
        // Por ahora, el frontend invalida queries de disponibilidad al recibir AvailabilityChanged.
    }

    private static HolidayDto Map(Holiday entity)
    {
        return new HolidayDto(
            entity.Id,
            entity.OrganizationId,
            entity.ClinicId,
            entity.Date,
            entity.Name,
            entity.Description,
            entity.IsActive,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}