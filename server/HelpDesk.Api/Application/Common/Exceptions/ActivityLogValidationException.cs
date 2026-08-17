namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Indicates invalid or unsafe activity-log input.</summary>
public sealed class ActivityLogValidationException : Exception
{
    public ActivityLogValidationException() : base("The activity log request is invalid.") { }
}
