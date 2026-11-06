namespace MedicalAppointments.Application.Common.Exceptions;

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("Invalid email or password.")
    {
    }
}

public class EmailNotConfirmedException : Exception
{
    public EmailNotConfirmedException()
        : base("The email address has not been confirmed.")
    {
    }
}

public class AccountLockedException : Exception
{
    public AccountLockedException()
        : base("The account is temporarily locked due to failed login attempts.")
    {
    }
}

public class TokenInvalidException : Exception
{
    public TokenInvalidException()
        : base("The token is invalid or has expired.")
    {
    }
}

public class RefreshTokenReuseDetectedException : Exception
{
    public RefreshTokenReuseDetectedException()
        : base("Refresh token reuse detected. The token family has been revoked.")
    {
    }
}