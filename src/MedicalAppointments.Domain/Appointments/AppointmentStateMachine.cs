using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.Domain.Appointments;

public static class AppointmentStateMachine
{
    private static readonly Dictionary<AppointmentStatus, HashSet<AppointmentStatus>> AllowedTransitions = new()
    {
        [AppointmentStatus.Pending] =
        [
            AppointmentStatus.Confirmed,
            AppointmentStatus.Cancelled,
            AppointmentStatus.Rescheduled
        ],

        [AppointmentStatus.Confirmed] =
        [
            AppointmentStatus.CheckedIn,
            AppointmentStatus.Cancelled,
            AppointmentStatus.Rescheduled,
            AppointmentStatus.NoShow
        ],

        [AppointmentStatus.CheckedIn] =
        [
            AppointmentStatus.InProgress,
            AppointmentStatus.NoShow
        ],

        [AppointmentStatus.InProgress] =
        [
            AppointmentStatus.Completed
        ],

        [AppointmentStatus.Completed] = [],
        [AppointmentStatus.Cancelled] = [],
        [AppointmentStatus.NoShow] = [],
        [AppointmentStatus.Rescheduled] = []
    };

    public static bool CanTransition(AppointmentStatus from, AppointmentStatus to)
    {
        return AllowedTransitions.TryGetValue(from, out var targets) && targets.Contains(to);
    }

    public static void EnsureTransition(AppointmentStatus from, AppointmentStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new DomainException($"Invalid appointment transition from {from} to {to}.");
        }
    }

    public static bool IsOccupyingDoctor(AppointmentStatus status)
    {
        return status
            is AppointmentStatus.Pending
            or AppointmentStatus.Confirmed
            or AppointmentStatus.CheckedIn
            or AppointmentStatus.InProgress;
    }

    public static bool IsTerminal(AppointmentStatus status)
    {
        return status
            is AppointmentStatus.Completed
            or AppointmentStatus.Cancelled
            or AppointmentStatus.NoShow
            or AppointmentStatus.Rescheduled;
    }
}