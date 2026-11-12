namespace MedicalAppointments.Application.Authorization;

public static partial class Policies
{
    public const string RealtimeTest = nameof(RealtimeTest);
    public const string FoundationTestWrite = nameof(FoundationTestWrite);
    public const string PrescriptionsRead = nameof(PrescriptionsRead);
    public const string PrescriptionsManage = nameof(PrescriptionsManage);
    public const string DocumentsRead = nameof(DocumentsRead);
    public const string DocumentsManage = nameof(DocumentsManage);
}
