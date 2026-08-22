namespace HelpDesk.Api.Application.Common.Exceptions;

public sealed class RoleManagementValidationException(string message) : Exception(message);
