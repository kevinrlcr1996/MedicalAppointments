using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Common;

public sealed record VitalSignsDto(
    Guid Id,
    Guid AppointmentId,
    Guid AuthorUserId,
    Guid AuthorDoctorId,
    string AuthorDisplayName,
    int? SystolicmmHg,
    int? DiastolicmmHg,
    int? HeartRateBpm,
    decimal? TemperatureC,
    VitalSignsTemperatureMethod? TemperatureMethod,
    VitalSignsPosition? Position,
    int? RespiratoryRatePerMin,
    int? SpO2Percent,
    decimal? WeightKg,
    decimal? HeightCm,
    int? PainScore0to10,
    string? Note,
    bool IsFinal,
    DateTimeOffset RecordedAtUtc,
    DateTimeOffset? FinalizedAt,
    Guid? FinalizedByUserId,
    Guid? CorrectsVitalSignsId,
    string? CorrectsVitalSignsTitle,
    DateTimeOffset CreatedAt);

public sealed record CreateVitalSignsCommand(Guid AppointmentId, VitalSignsMeasurements Measurements, DateTimeOffset? RecordedAtUtc);
public sealed record UpdateVitalSignsCommand(Guid Id, VitalSignsMeasurements Measurements);
public sealed record CorrectVitalSignsCommand(Guid OriginalId, VitalSignsMeasurements Measurements, DateTimeOffset? RecordedAtUtc);