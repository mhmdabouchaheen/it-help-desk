namespace HelpDesk.Api.Application.Common.Exceptions;

/// <summary>Indicates invalid notification application input.</summary>
public sealed class NotificationValidationException() : Exception("The notification request is invalid.");
