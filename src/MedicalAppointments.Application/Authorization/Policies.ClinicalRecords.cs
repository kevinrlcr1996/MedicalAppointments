namespace MedicalAppointments.Application.Authorization;

public static partial class Policies
{
    public const string ClinicalRecordsRead = nameof(ClinicalRecordsRead);
    public const string ClinicalRecordsManage = nameof(ClinicalRecordsManage);
}