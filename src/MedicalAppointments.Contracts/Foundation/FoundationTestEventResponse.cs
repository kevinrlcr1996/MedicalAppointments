namespace MedicalAppointments.Contracts.Foundation;

public sealed record FoundationTestEventResponse(
    Guid OutboxMessageId,
    string Status);