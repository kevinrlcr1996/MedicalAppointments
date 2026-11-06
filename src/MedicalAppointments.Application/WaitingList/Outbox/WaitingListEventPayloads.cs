using MedicalAppointments.Domain.WaitingList;

namespace MedicalAppointments.Application.WaitingList.Outbox;

public sealed record SlotFreedPayload(
    Guid DoctorId,
    Guid? ClinicId,
    Guid? RoomId,
    DateOnly LocalDate,
    DateTimeOffset SlotStartUtc,
    DateTimeOffset SlotEndUtc,
    DateTimeOffset OccurredAt,
    string? CorrelationId);

public sealed record OfferCreatedPayload(
    Guid OfferId,
    Guid EntryId,
    Guid PatientId,
    Guid? PatientUserId,
    Guid DoctorId,
    Guid? ClinicId,
    DateTimeOffset SlotStartUtc,
    DateTimeOffset SlotEndUtc,
    DateTimeOffset ExpiresAt,
    DateTimeOffset OccurredAt,
    string? CorrelationId);

public sealed record WaitingListUpdatedPayload(
    Guid? EntryId,
    Guid? OfferId,
    Guid? PatientId,
    Guid? PatientUserId,
    Guid DoctorId,
    Guid? ClinicId,
    string ChangeType,
    WaitingListEntryStatus? EntryStatus,
    WaitingListOfferStatus? OfferStatus,
    DateTimeOffset OccurredAt,
    string? CorrelationId);