namespace MedicalAppointments.Domain.ClinicalRecords;

public enum AllergenClass
{
    Unknown = 0,
    Drug = 1,
    Food = 2,
    Latex = 3,
    Environmental = 4,
    InsectVenom = 5,
    Other = 6
}

public enum AllergySeverity
{
    Unknown = 0,
    Mild = 1,
    Moderate = 2,
    Severe = 3
}