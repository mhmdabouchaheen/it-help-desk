namespace HelpDesk.Api.Application.Common.Exceptions;

public sealed class TeamManagementValidationException(string message) : Exception(message);
