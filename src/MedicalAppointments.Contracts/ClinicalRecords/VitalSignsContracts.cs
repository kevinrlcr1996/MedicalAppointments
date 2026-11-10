using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Contracts.ClinicalRecords;

public sealed record VitalSignsResponse(
    Guid Id, Guid AppointmentId, Guid AuthorUserId, Guid AuthorDoctorId, string AuthorDisplayName,
    int? SystolicmmHg, int? DiastolicmmHg, int? HeartRateBpm, decimal? TemperatureC,
    VitalSignsTemperatureMethod? TemperatureMethod, VitalSignsPosition? Position,
    int? RespiratoryRatePerMin, int? SpO2Percent, decimal? WeightKg, decimal? HeightCm,
    int? PainScore0to10, string? Note, bool IsFinal, DateTimeOffset RecordedAtUtc,
    DateTimeOffset? FinalizedAt, Guid? FinalizedByUserId, Guid? CorrectsVitalSignsId,
    string? CorrectsVitalSignsTitle, DateTimeOffset CreatedAt);

public sealed record CreateVitalSignsRequest(
    int? SystolicmmHg, int? DiastolicmmHg, int? HeartRateBpm, decimal? TemperatureC,
    VitalSignsTemperatureMethod? TemperatureMethod, VitalSignsPosition? Position,
    int? RespiratoryRatePerMin, int? SpO2Percent, decimal? WeightKg, decimal? HeightCm,
    int? PainScore0to10, string? Note, DateTimeOffset? RecordedAtUtc);

public sealed record UpdateVitalSignsRequest(
    int? SystolicmmHg, int? DiastolicmmHg, int? HeartRateBpm, decimal? TemperatureC,
    VitalSignsTemperatureMethod? TemperatureMethod, VitalSignsPosition? Position,
    int? RespiratoryRatePerMin, int? SpO2Percent, decimal? WeightKg, decimal? HeightCm,
    int? PainScore0to10, string? Note);

public sealed record CorrectVitalSignsRequest(
    int? SystolicmmHg, int? DiastolicmmHg, int? HeartRateBpm, decimal? TemperatureC,
    VitalSignsTemperatureMethod? TemperatureMethod, VitalSignsPosition? Position,
    int? RespiratoryRatePerMin, int? SpO2Percent, decimal? WeightKg, decimal? HeightCm,
    int? PainScore0to10, string? Note, DateTimeOffset? RecordedAtUtc);