namespace MedicalAppointments.Application.Authorization;

public static partial class Policies
{
    public const string WaitingRoomRead = nameof(WaitingRoomRead);
    public const string WaitingRoomOperate = nameof(WaitingRoomOperate);
}