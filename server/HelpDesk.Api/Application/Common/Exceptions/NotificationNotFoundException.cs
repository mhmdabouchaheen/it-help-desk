namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Indicates that a recipient-owned notification could not be found.</summary>
public sealed class NotificationNotFoundException() : Exception("The notification was not found.");
