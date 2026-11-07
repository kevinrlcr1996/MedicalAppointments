using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.WaitingRoom;

namespace MedicalAppointments.Application.WaitingRoom.Common;

internal static class WaitingRoomGuards
{
    // Estricto: usado por Skip y Answer (exigen Called real).
    public static void EnsureCalled(this WaitingRoomCall call)
    {
        if (call.Status != WaitingRoomCallStatus.Called)
        {
            throw new DomainException($"Call is not in Called state (current: {call.Status}).");
        }
    }

    // FIX: usado por Recall (re-anunciar un Llamado o re-llamar un Saltado).
    public static void EnsureCalledOrSkipped(this WaitingRoomCall call)
    {
        if (call.Status is not (WaitingRoomCallStatus.Called or WaitingRoomCallStatus.Skipped))
        {
            throw new DomainException($"Call cannot be recalled from state {call.Status}.");
        }
    }

    public static void EnsureInConsultationOrCalled(this WaitingRoomCall call)
    {
        if (call.Status is not (WaitingRoomCallStatus.InConsultation or WaitingRoomCallStatus.Called))
        {
            throw new DomainException($"Call cannot be completed from state {call.Status}.");
        }
    }

    public static void MarkAnsweredOrThrow(this WaitingRoomCall call)
    {
        call.EnsureCalled();
    }
}

internal static class PatientNameExtensions
{
    public static string FullName(this MedicalAppointments.Domain.MedicalStructure.Patient patient)
    {
        return $"{patient.FirstName} {patient.LastName}";
    }
}