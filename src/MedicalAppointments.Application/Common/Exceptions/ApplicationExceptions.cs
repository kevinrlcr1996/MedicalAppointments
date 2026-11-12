namespace MedicalAppointments.Application.Common.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}

public sealed class AttachmentScannerUnavailableException : Exception
{
    public AttachmentScannerUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
