using MedicalAppointments.Domain.WaitingList;

namespace MedicalAppointments.Contracts.WaitingList;

public sealed record WaitingListEntryResponse(
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

public sealed record WaitingListOfferResponse(
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

public sealed record CreateWaitingListEntryRequest(
    Guid PatientId,
    Guid DoctorId,
    Guid? ClinicId,
    DateTimeOffset PreferredFromUtc,
    DateTimeOffset PreferredToUtc,
    WaitingListPriority Priority,
    string? Note);

public sealed record UpdateWaitingListEntryRequest(
    Guid? ClinicId,
    DateTimeOffset PreferredFromUtc,
    DateTimeOffset PreferredToUtc,
    WaitingListPriority Priority,
    string? Note);

public sealed record RespondOfferRequest(bool Accept);