namespace MedicalAppointments.Application.Authorization;

public static partial class Policies
{
    public const string WaitingListRead = nameof(WaitingListRead);
    public const string WaitingListManage = nameof(WaitingListManage);
}