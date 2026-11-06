using System.Security.Claims;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Application.MedicalStructure.Patients;

namespace MedicalAppointments.Application.WaitingList.Common;

public sealed class WaitingListAccessService : IWaitingListAccessService
{
    private readonly ICurrentPrincipalProvider _principalProvider;
    private readonly IPatientStore _patientStore;
    private readonly IDoctorStore _doctorStore;

    public WaitingListAccessService(
        ICurrentPrincipalProvider principalProvider,
        IPatientStore patientStore,
        IDoctorStore doctorStore)
    {
        _principalProvider = principalProvider;
        _patientStore = patientStore;
        _doctorStore = doctorStore;
    }

    public async Task<WaitingListAccessSubject> GetSubjectAsync(CancellationToken cancellationToken = default)
    {
        var principal = _principalProvider.GetCurrentPrincipal()
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var userId = Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedUserId)
            ? parsedUserId
            : (Guid?)null;

        var canReadAll = principal.HasPermission(Permissions.WaitingListReadAll);
        var canReadAssigned = principal.HasPermission(Permissions.WaitingListReadAssigned);
        var canReadOwn = principal.HasPermission(Permissions.WaitingListReadOwn);

        var canManageAll = principal.HasPermission(Permissions.WaitingListManageAll);
        var canManageOwn = principal.HasPermission(Permissions.WaitingListManageOwn);

        var needsDoctorId = canReadAssigned || canManageAll is false && canReadAssigned;
        var needsPatientId = canReadOwn || canManageOwn;

        Guid? doctorId = null;
        Guid? patientId = null;

        if (userId.HasValue && (canReadAssigned || canManageAll))
        {
            doctorId = await _doctorStore.GetIdByUserIdAsync(userId.Value, cancellationToken);
        }

        if (userId.HasValue && needsPatientId)
        {
            patientId = await _patientStore.GetIdByUserIdAsync(userId.Value, cancellationToken);
        }

        return new WaitingListAccessSubject(
            UserId: userId,
            PatientId: patientId,
            DoctorId: doctorId,
            CanReadAll: canReadAll,
            CanReadAssigned: canReadAssigned,
            CanReadOwn: canReadOwn,
            CanManageAll: canManageAll,
            CanManageAssigned: false,
            CanManageOwn: canManageOwn);
    }

    public WaitingListEntryReadFilter BuildEntryReadFilter(WaitingListAccessSubject subject)
    {
        if (subject.CanReadAll)
        {
            return new WaitingListEntryReadFilter(true, Array.Empty<Guid>(), Array.Empty<Guid>(), false);
        }

        var canReadAny = subject.CanReadAssigned || subject.CanReadOwn;
        if (!canReadAny)
        {
            return new WaitingListEntryReadFilter(false, Array.Empty<Guid>(), Array.Empty<Guid>(), true);
        }

        var doctorIds = new List<Guid>();
        var patientIds = new List<Guid>();

        if (subject.CanReadAssigned && subject.DoctorId.HasValue)
        {
            doctorIds.Add(subject.DoctorId.Value);
        }

        if (subject.CanReadOwn && subject.PatientId.HasValue)
        {
            patientIds.Add(subject.PatientId.Value);
        }

        return new WaitingListEntryReadFilter(false, doctorIds, patientIds, false);
    }

    public bool CanReadEntry(WaitingListEntryDto entry, WaitingListAccessSubject subject)
    {
        return subject.CanReadAll
            || (subject.CanReadAssigned && subject.DoctorId == entry.DoctorId)
            || (subject.CanReadOwn && subject.PatientId == entry.PatientId);
    }

    public bool CanCreateEntry(CreateWaitingListEntryCommand command, WaitingListAccessSubject subject)
    {
        return subject.CanManageAll
            || (subject.CanManageOwn && subject.PatientId == command.PatientId);
    }

    public bool CanManageEntry(WaitingListEntryDto entry, WaitingListAccessSubject subject)
    {
        return subject.CanManageAll
            || (subject.CanManageOwn && subject.PatientId == entry.PatientId);
    }

    public bool CanRespondOffer(WaitingListOfferDto offer, WaitingListAccessSubject subject)
    {
        return subject.CanManageAll
            || (subject.CanManageOwn && subject.PatientId == offer.PatientId);
    }
}