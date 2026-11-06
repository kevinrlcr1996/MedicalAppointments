namespace MedicalAppointments.Application.Appointments;

public sealed class AppointmentBookingOptions
{
    public int MinCancellationHours { get; set; } = 2;

    public int MinRescheduleHours { get; set; } = 2;

    public int MaxAdvanceDays { get; set; } = 90;

    public TimeSpan IdempotencyTtl { get; set; } = TimeSpan.FromHours(24);
}