using MedicalAppointments.Domain.Appointments;

namespace MedicalAppointments.UnitTests.Domain;

public class AppointmentStateMachineTests
{
    [Theory]
    [InlineData(AppointmentStatus.Pending, AppointmentStatus.Confirmed, true)]
    [InlineData(AppointmentStatus.Pending, AppointmentStatus.Cancelled, true)]
    [InlineData(AppointmentStatus.Pending, AppointmentStatus.Completed, false)]
    [InlineData(AppointmentStatus.Confirmed, AppointmentStatus.CheckedIn, true)]
    [InlineData(AppointmentStatus.Confirmed, AppointmentStatus.Completed, false)]
    [InlineData(AppointmentStatus.CheckedIn, AppointmentStatus.InProgress, true)]
    [InlineData(AppointmentStatus.InProgress, AppointmentStatus.Completed, true)]
    [InlineData(AppointmentStatus.Completed, AppointmentStatus.Cancelled, false)]
    public void CanTransition_Should_Follow_Rules(
        AppointmentStatus from,
        AppointmentStatus to,
        bool expected)
    {
        Assert.Equal(expected, AppointmentStateMachine.CanTransition(from, to));
    }
}