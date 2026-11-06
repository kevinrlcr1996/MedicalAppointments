using MedicalAppointments.Domain.WaitingList;

namespace MedicalAppointments.Application.WaitingList.Common;

public sealed record WaitingListEntryDto(
    Guid Id,
    Guid PatientId,
    string PatientFullName,
    Guid DoctorId,
    string DoctorFullName,
    Guid? ClinicId,
    string? ClinicName,
    DateTimeOffset PreferredFromUtc,
    DateTimeOffset PreferredToUtc,
    WaitingListPriority Priority,
    WaitingListEntryStatus Status,
    string? Note,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record WaitingListOfferDto(
    Guid Id,
    Guid EntryId,
    Guid PatientId,
    Guid DoctorId,
    string DoctorFullName,
    Guid? ClinicId,
    string? ClinicName,
    Guid? RoomId,
    DateOnly LocalDate,
    DateTimeOffset SlotStartUtc,
    DateTimeOffset SlotEndUtc,
    WaitingListOfferStatus Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RespondedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateWaitingListEntryCommand(
    Guid PatientId,
    Guid DoctorId,
    Guid? ClinicId,
    DateTimeOffset PreferredFromUtc,
    DateTimeOffset PreferredToUtc,
    WaitingListPriority Priority,
    string? Note);

public sealed record UpdateWaitingListEntryCommand(
    Guid Id,
    Guid? ClinicId,
    DateTimeOffset PreferredFromUtc,
    DateTimeOffset PreferredToUtc,
    WaitingListPriority Priority,
    string? Note);

public sealed record CancelWaitingListEntryCommand(Guid Id);

public sealed record RespondOfferCommand(Guid OfferId, bool Accept);

public sealed record WaitingListEntryQuery(
    Guid? PatientId,
    Guid? DoctorId,
    Guid? ClinicId,
    WaitingListEntryStatus? Status,
    int Page,
    int PageSize);

public sealed record WaitingListOfferQuery(
    Guid? PatientId,
    Guid? DoctorId,
    Guid? ClinicId,
    WaitingListOfferStatus? Status,
    int Page,
    int PageSize);