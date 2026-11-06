using FluentValidation;
using MedicalAppointments.Application.Auth;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Application.MedicalStructure.Patients;

public sealed class PatientService : IPatientService
{
    private readonly IPatientStore _store;
    private readonly IUserStore _userStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<CreatePatientCommand> _createValidator;
    private readonly IValidator<UpdatePatientCommand> _updateValidator;
    private readonly IValidator<CreateSelfPatientCommand> _createSelfValidator;
    private readonly IValidator<UpdateSelfPatientCommand> _updateSelfValidator;

    public PatientService(
        IPatientStore store,
        IUserStore userStore,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IValidator<CreatePatientCommand> createValidator,
        IValidator<UpdatePatientCommand> updateValidator,
        IValidator<CreateSelfPatientCommand> createSelfValidator,
        IValidator<UpdateSelfPatientCommand> updateSelfValidator)
    {
        _store = store;
        _userStore = userStore;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _createSelfValidator = createSelfValidator;
        _updateSelfValidator = updateSelfValidator;
    }

    public async Task<PagedResult<PatientDto>> GetPagedAsync(
        PatientQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _store.GetPagedAsync(query, cancellationToken);

        return new PagedResult<PatientDto>(
            result.Items.Select(Map).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages);
    }

    public async Task<PatientDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var entity = await _store.GetTrackedByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Patient not found.");

        return Map(entity);
    }

    public async Task<PatientDto?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var patientId = await _store.GetIdByUserIdAsync(userId, cancellationToken);

        if (patientId is null)
        {
            return null;
        }

        var entity = await _store.GetTrackedByIdAsync(patientId.Value, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task<PatientDto> CreateAsync(
        CreatePatientCommand command,
        CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();

        if (command.UserId.HasValue &&
            !await _userStore.ExistsAsync(command.UserId.Value, cancellationToken))
        {
            throw new NotFoundException("Linked user not found.");
        }

        var normalizedIdentification = StringNormalizer.NormalizeUpperOrNull(command.IdentificationNumber);

        if (normalizedIdentification is not null &&
            await _store.ExistsNormalizedIdentificationAsync(normalizedIdentification, null, cancellationToken))
        {
            throw new ConflictException("A patient with this identification number already exists.");
        }

        if (command.UserId.HasValue &&
            await _store.ExistsUserIdAsync(command.UserId.Value, null, cancellationToken))
        {
            throw new ConflictException("A patient profile is already linked to this user.");
        }

        var entity = Patient.Create(
            command.UserId,
            command.FirstName,
            command.LastName,
            command.DateOfBirth,
            command.Gender,
            command.IdentificationNumber,
            command.Phone,
            command.Email,
            command.Address,
            command.EmergencyContact,
            command.EmergencyPhone,
            command.BloodType,
            command.InsuranceProvider,
            command.InsuranceNumber,
            now);

        await _store.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<PatientDto> UpdateAsync(
        UpdatePatientCommand command,
        CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        var entity = await _store.GetTrackedByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Patient not found.");

        if (command.UserId.HasValue &&
            !await _userStore.ExistsAsync(command.UserId.Value, cancellationToken))
        {
            throw new NotFoundException("Linked user not found.");
        }

        var normalizedIdentification = StringNormalizer.NormalizeUpperOrNull(command.IdentificationNumber);

        if (normalizedIdentification is not null &&
            await _store.ExistsNormalizedIdentificationAsync(normalizedIdentification, command.Id, cancellationToken))
        {
            throw new ConflictException("A patient with this identification number already exists.");
        }

        if (command.UserId.HasValue &&
            await _store.ExistsUserIdAsync(command.UserId.Value, command.Id, cancellationToken))
        {
            throw new ConflictException("A patient profile is already linked to this user.");
        }

        entity.UpdateProfile(
            command.UserId,
            command.FirstName,
            command.LastName,
            command.DateOfBirth,
            command.Gender,
            command.IdentificationNumber,
            command.Phone,
            command.Email,
            command.Address,
            command.EmergencyContact,
            command.EmergencyPhone,
            command.BloodType,
            command.InsuranceProvider,
            command.InsuranceNumber,
            now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<PatientDto> CreateSelfAsync(
        CreateSelfPatientCommand command,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        await _createSelfValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();

        // Un usuario solo puede tener UNA ficha. Evita duplicados y escalada.
        if (await _store.ExistsUserIdAsync(currentUserId, null, cancellationToken))
        {
            throw new ConflictException("This user already has a patient profile.");
        }

        var normalizedIdentification = StringNormalizer.NormalizeUpperOrNull(command.IdentificationNumber);

        if (normalizedIdentification is not null &&
            await _store.ExistsNormalizedIdentificationAsync(normalizedIdentification, null, cancellationToken))
        {
            throw new ConflictException("A patient with this identification number already exists.");
        }

        // El UserId SIEMPRE es el del token. El body no manda identidad.
        var entity = Patient.Create(
            userId: currentUserId,
            firstName: command.FirstName,
            lastName: command.LastName,
            dateOfBirth: command.DateOfBirth,
            gender: command.Gender,
            identificationNumber: command.IdentificationNumber,
            phone: command.Phone,
            email: command.Email,
            address: command.Address,
            emergencyContact: command.EmergencyContact,
            emergencyPhone: command.EmergencyPhone,
            bloodType: command.BloodType,
            insuranceProvider: command.InsuranceProvider,
            insuranceNumber: command.InsuranceNumber,
            now: now);

        await _store.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    public async Task<PatientDto> UpdateSelfAsync(
        UpdateSelfPatientCommand command,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        await _updateSelfValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();

        var patientId = await _store.GetIdByUserIdAsync(currentUserId, cancellationToken)
            ?? throw new NotFoundException("Patient profile not found.");

        var entity = await _store.GetTrackedByIdAsync(patientId, cancellationToken)
            ?? throw new NotFoundException("Patient profile not found.");

        var normalizedIdentification = StringNormalizer.NormalizeUpperOrNull(command.IdentificationNumber);

        if (normalizedIdentification is not null &&
            await _store.ExistsNormalizedIdentificationAsync(normalizedIdentification, patientId, cancellationToken))
        {
            throw new ConflictException("A patient with this identification number already exists.");
        }

        // Se mantiene el vínculo al propio usuario; no se permite re-asignar.
        entity.UpdateProfile(
            userId: currentUserId,
            firstName: command.FirstName,
            lastName: command.LastName,
            dateOfBirth: command.DateOfBirth,
            gender: command.Gender,
            identificationNumber: command.IdentificationNumber,
            phone: command.Phone,
            email: command.Email,
            address: command.Address,
            emergencyContact: command.EmergencyContact,
            emergencyPhone: command.EmergencyPhone,
            bloodType: command.BloodType,
            insuranceProvider: command.InsuranceProvider,
            insuranceNumber: command.InsuranceNumber,
            now: now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(entity);
    }

    private static PatientDto Map(Patient entity)
    {
        return new PatientDto(
            entity.Id,
            entity.UserId,
            entity.FirstName,
            entity.LastName,
            entity.DateOfBirth,
            entity.Gender,
            entity.IdentificationNumber,
            entity.Phone,
            entity.Email,
            entity.Address,
            entity.EmergencyContact,
            entity.EmergencyPhone,
            entity.BloodType,
            entity.InsuranceProvider,
            entity.InsuranceNumber,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}