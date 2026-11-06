using FluentValidation;
using MedicalAppointments.Application.Auth;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.MedicalStructure.Clinics;
using MedicalAppointments.Application.MedicalStructure.Specialties;
using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Application.MedicalStructure.Doctors;

public sealed class DoctorService : IDoctorService
{
    private readonly IDoctorStore _store;
    private readonly ISpecialtyStore _specialtyStore;
    private readonly IClinicStore _clinicStore;
    private readonly IUserStore _userStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<CreateDoctorCommand> _createValidator;
    private readonly IValidator<UpdateDoctorCommand> _updateValidator;

    public DoctorService(
        IDoctorStore store,
        ISpecialtyStore specialtyStore,
        IClinicStore clinicStore,
        IUserStore userStore,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IValidator<CreateDoctorCommand> createValidator,
        IValidator<UpdateDoctorCommand> updateValidator)
    {
        _store = store;
        _specialtyStore = specialtyStore;
        _clinicStore = clinicStore;
        _userStore = userStore;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<PagedResult<DoctorDto>> GetPagedAsync(
        DoctorQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _store.GetPagedAsync(query, cancellationToken);

        return new PagedResult<DoctorDto>(
            result.Items.Select(Map).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
    }

    public async Task<DoctorDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _store.GetTrackedWithRelationsAsync(id, cancellationToken)
            ?? throw new NotFoundException("Doctor not found.");

        return Map(entity);
    }

    public async Task<DoctorDto> CreateAsync(
        CreateDoctorCommand command,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();

        await ValidateReferencesAsync(command, null, cancellationToken);

        var normalizedLicenseNumber = StringNormalizer.NormalizeUpper(command.LicenseNumber);
        var normalizedLicenseCountry = StringNormalizer.NormalizeUpper(command.LicenseCountry);

        if (await _store.ExistsNormalizedLicenseAsync(
                normalizedLicenseNumber,
                normalizedLicenseCountry,
                null,
                cancellationToken))
        {
            throw new ConflictException("A doctor with this license already exists.");
        }

        if (command.UserId.HasValue &&
            await _store.ExistsUserIdAsync(command.UserId.Value, null, cancellationToken))
        {
            throw new ConflictException("A doctor profile is already linked to this user.");
        }

        var entity = Doctor.Create(
            command.UserId,
            command.FirstName,
            command.LastName,
            command.Email,
            command.Phone,
            command.LicenseNumber,
            command.LicenseCountry,
            command.Biography,
            command.PhotoUrl,
            command.Gender,
            command.DateOfBirth,
            command.Status,
            now);

        ApplyRelations(entity, command, now);

        await _store.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<DoctorDto> UpdateAsync(
        UpdateDoctorCommand command,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        var entity = await _store.GetTrackedWithRelationsAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Doctor not found.");

        await ValidateReferencesAsync(command, command.Id, cancellationToken);

        var normalizedLicenseNumber = StringNormalizer.NormalizeUpper(command.LicenseNumber);
        var normalizedLicenseCountry = StringNormalizer.NormalizeUpper(command.LicenseCountry);

        if (await _store.ExistsNormalizedLicenseAsync(
                normalizedLicenseNumber,
                normalizedLicenseCountry,
                command.Id,
                cancellationToken))
        {
            throw new ConflictException("A doctor with this license already exists.");
        }

        if (command.UserId.HasValue &&
            await _store.ExistsUserIdAsync(command.UserId.Value, command.Id, cancellationToken))
        {
            throw new ConflictException("A doctor profile is already linked to this user.");
        }

        entity.UpdateProfile(
            command.UserId,
            command.FirstName,
            command.LastName,
            command.Email,
            command.Phone,
            command.LicenseNumber,
            command.LicenseCountry,
            command.Biography,
            command.PhotoUrl,
            command.Gender,
            command.DateOfBirth,
            command.Status,
            now);

        ApplyRelations(entity, command, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    private async Task ValidateReferencesAsync(
        CreateDoctorCommand command,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        if (command.UserId.HasValue &&
            !await _userStore.ExistsAsync(command.UserId.Value, cancellationToken))
        {
            throw new NotFoundException("Linked user not found.");
        }

        if (command.SpecialtyIds.Count > 0 &&
            !await _specialtyStore.ExistsAsync(command.SpecialtyIds, cancellationToken))
        {
            throw new NotFoundException("One or more specialties were not found.");
        }

        var clinicIds = command.Clinics.Select(x => x.ClinicId).Distinct().ToList();

        if (clinicIds.Count > 0 &&
            !await _clinicStore.ExistsAsync(clinicIds, cancellationToken))
        {
            throw new NotFoundException("One or more clinics were not found.");
        }
    }

    private Task ValidateReferencesAsync(
        UpdateDoctorCommand command,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        return ValidateReferencesAsync(
            new CreateDoctorCommand(
                command.UserId,
                command.FirstName,
                command.LastName,
                command.Email,
                command.Phone,
                command.LicenseNumber,
                command.LicenseCountry,
                command.Biography,
                command.PhotoUrl,
                command.Gender,
                command.DateOfBirth,
                command.Status,
                command.SpecialtyIds,
                command.Clinics,
                command.Languages),
            excludeId,
            cancellationToken);
    }

    private static void ApplyRelations(
        Doctor entity,
        CreateDoctorCommand command,
        DateTimeOffset now)
    {
        entity.ReplaceSpecialties(command.SpecialtyIds, now);

        entity.ReplaceClinics(
            command.Clinics.Select(x => new DoctorClinicAssignment(
                x.ClinicId,
                x.IsPrimary,
                x.StartDate,
                x.EndDate)),
            now);

        entity.ReplaceLanguages(command.Languages, now);
    }

    private static void ApplyRelations(
        Doctor entity,
        UpdateDoctorCommand command,
        DateTimeOffset now)
    {
        ApplyRelations(
            entity,
            new CreateDoctorCommand(
                command.UserId,
                command.FirstName,
                command.LastName,
                command.Email,
                command.Phone,
                command.LicenseNumber,
                command.LicenseCountry,
                command.Biography,
                command.PhotoUrl,
                command.Gender,
                command.DateOfBirth,
                command.Status,
                command.SpecialtyIds,
                command.Clinics,
                command.Languages),
            now);
    }

    private static DoctorDto Map(Doctor entity)
    {
        return new DoctorDto(
            entity.Id,
            entity.UserId,
            entity.FirstName,
            entity.LastName,
            entity.Email,
            entity.Phone,
            entity.LicenseNumber,
            entity.LicenseCountry,
            entity.Biography,
            entity.PhotoUrl,
            entity.Gender,
            entity.DateOfBirth,
            entity.Status,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.Specialties
                .Select(x => new DoctorSpecialtyDto(
                    x.SpecialtyId,
                    x.Specialty?.Name ?? string.Empty,
                    x.Specialty?.Code))
                .ToList(),
            entity.Clinics
                .Select(x => new DoctorClinicDto(
                    x.ClinicId,
                    x.Clinic?.Name ?? string.Empty,
                    x.Clinic?.Code,
                    x.IsPrimary,
                    x.StartDate,
                    x.EndDate))
                .ToList(),
            entity.Languages
                .Select(x => x.Language)
                .ToList());
    }
}