namespace MedicalAppointments.Application.Common.Exceptions;

public class PolicyViolationException : Exception
{
    public PolicyViolationException(string message) : base(message)
    {
    }
}