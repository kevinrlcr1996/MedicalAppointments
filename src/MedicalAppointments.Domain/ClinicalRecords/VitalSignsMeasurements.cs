namespace MedicalAppointments.Domain.ClinicalRecords;

public sealed record VitalSignsMeasurements(
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
    string? Note);