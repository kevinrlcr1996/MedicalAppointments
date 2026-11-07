using MedicalAppointments.Domain.WaitingRoom;

namespace MedicalAppointments.Application.WaitingRoom.Outbox;

public interface IWaitingRoomEventNotifier
{
    void EnqueueUpdated(
        WaitingRoomCall call,
        string changeType,
        DateTimeOffset now,
        string? correlationId);

    void EnqueuePatientCalled(
        WaitingRoomCall call,
        Guid patientId,
        Guid? patientUserId,
        string patientFullName,
        string doctorFullName,
        string? roomName,
        DateTimeOffset now,
        string? correlationId);

    void EnqueuePatientRecalled(
        WaitingRoomCall call,
        Guid patientId,
        Guid? patientUserId,
        string patientFullName,
        string? roomName,
        DateTimeOffset now,
        string? correlationId);
}