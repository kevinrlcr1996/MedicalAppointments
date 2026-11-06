namespace MedicalAppointments.Contracts.Auth;

public sealed record DevTokenRequestDto(
    string? UserName,
    Guid? UserId);