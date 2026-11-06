namespace MedicalAppointments.Application.Realtime;

public static class RealtimeGroups
{
    public const string TestFoundation = "test:foundation";

    public static string ForUser(Guid userId)
    {
        return $"user:{userId}";
    }

    public static string ForDoctor(Guid doctorId)
    {
        return $"doctor:{doctorId}";
    }

    public static string ForClinic(Guid clinicId)
    {
        return $"clinic:{clinicId}";
    }

    public static string ForPatient(Guid patientId)
    {
        return $"patient:{patientId}";
    }

    public static string ForAppointment(Guid appointmentId)
    {
        return $"appointment:{appointmentId}";
    }
}