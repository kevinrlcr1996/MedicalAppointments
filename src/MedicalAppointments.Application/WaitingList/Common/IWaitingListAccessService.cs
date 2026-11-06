namespace MedicalAppointments.Application.WaitingList.Common;

public sealed record WaitingListAccessSubject(
    Guid? UserId,
    Guid? PatientId,
    Guid? DoctorId,
    bool CanReadAll,
    bool CanReadAssigned,
    bool CanReadOwn,
    bool CanManageAll,
    bool CanManageAssigned,
    bool CanManageOwn);

public sealed record WaitingListEntryReadFilter(
    bool All,
    IReadOnlyCollection<Guid> DoctorIds,
    IReadOnlyCollection<Guid> PatientIds,
    bool Denied);

public interface IWaitingListAccessService
{
    Task<WaitingListAccessSubject> GetSubjectAsync(CancellationToken cancellationToken = default);

    WaitingListEntryReadFilter BuildEntryReadFilter(WaitingListAccessSubject subject);

    bool CanReadEntry(WaitingListEntryDto entry, WaitingListAccessSubject subject);

    bool CanCreateEntry(CreateWaitingListEntryCommand command, WaitingListAccessSubject subject);

    bool CanManageEntry(WaitingListEntryDto entry, WaitingListAccessSubject subject);

    bool CanRespondOffer(WaitingListOfferDto offer, WaitingListAccessSubject subject);
}