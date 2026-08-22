namespace HelpDesk.Api.Application.Common.Exceptions;

public sealed class InvalidPasswordResetException : Exception
{
    public InvalidPasswordResetException() : base("The password reset request is invalid or has expired.") { }
}

public sealed class ProfileValidationException : Exception
{
    public ProfileValidationException(string message = "The profile change could not be completed.") : base(message) { }
}
