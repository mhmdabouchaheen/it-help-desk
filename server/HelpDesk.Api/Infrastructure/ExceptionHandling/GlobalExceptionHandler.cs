using HelpDesk.Api.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace HelpDesk.Api.Infrastructure.ExceptionHandling;

/// <summary>
/// Converts application exceptions into stable, non-sensitive ProblemDetails responses.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var mapping = MapException(exception);

        if (mapping.IsServerError)
        {
            logger.LogError(
                "Request failed with an unexpected server error of type {ExceptionType}. Trace ID: {TraceId}.",
                exception.GetType().Name,
                httpContext.TraceIdentifier);
        }
        else
        {
            logger.LogInformation(
                "Request rejected with problem code {ProblemCode}. Trace ID: {TraceId}.",
                mapping.Code,
                httpContext.TraceIdentifier);
        }

        var problem = new ProblemDetails
        {
            Status = mapping.Status,
            Title = mapping.Title,
            Detail = mapping.Detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["code"] = mapping.Code;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = mapping.Status;
        httpContext.Response.ContentType = "application/problem+json";
        var jsonOptions = httpContext.RequestServices
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()
            .Value.SerializerOptions;
        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            problem,
            jsonOptions,
            cancellationToken: cancellationToken);
        return true;
    }

    private static ExceptionMapping MapException(Exception exception) => exception switch
    {
        EmailAlreadyRegisteredException => new(409, "Email already registered", exception.Message, "email_already_registered"),
        AuthenticationFailedException => new(401, "Authentication failed", exception.Message, "authentication_failed"),
        UserInactiveException => new(403, "User inactive", exception.Message, "user_inactive"),
        UserNotFoundException => new(404, "User not found", exception.Message, "user_not_found"),
        InvalidRefreshTokenException => new(401, "Invalid refresh token", exception.Message, "invalid_refresh_token"),
        RefreshTokenReuseDetectedException => new(401, "Refresh token rejected", exception.Message, "refresh_token_reuse_detected"),
        InvalidPasswordResetException => new(400, "Password reset failed", exception.Message, "invalid_password_reset"),
        ProfileValidationException => new(400, "Profile validation failed", exception.Message, "profile_validation_failed"),
        TeamManagementValidationException => new(400, "Team assignment failed", exception.Message, "team_assignment_failed"),
        UserRegistrationException => new(400, "Registration failed", exception.Message, "registration_failed"),
        AuthenticationTokenIssuanceException => new(500, "Authentication service unavailable", "Authentication credentials could not be issued.", "token_issuance_failed", true),
        InvalidAuthenticatedPrincipalException => new(401, "Invalid authenticated principal", exception.Message, "invalid_authenticated_principal"),
        TicketNotFoundException => new(404, "Ticket not found", exception.Message, "ticket_not_found"),
        CategoryNotFoundException => new(404, "Category not found", exception.Message, "category_not_found"),
        PriorityNotFoundException => new(404, "Priority not found", exception.Message, "priority_not_found"),
        StatusNotFoundException => new(404, "Status not found", exception.Message, "status_not_found"),
        AssignmentTargetNotFoundException => new(404, "Assignment target not found", exception.Message, "assignment_target_not_found"),
        TicketAccessDeniedException => new(403, "Ticket access denied", exception.Message, "ticket_access_denied"),
        TicketValidationException => new(400, "Ticket validation failed", exception.Message, "ticket_validation_failed"),
        TicketStateConflictException => new(409, "Ticket state conflict", exception.Message, "ticket_state_conflict"),
        AttachmentNotFoundException => new(404, "Attachment not found", exception.Message, "attachment_not_found"),
        AttachmentAccessDeniedException => new(403, "Attachment access denied", exception.Message, "attachment_access_denied"),
        AttachmentValidationException => new(400, "Attachment validation failed", exception.Message, "attachment_validation_failed"),
        AttachmentTooLargeException => new(413, "Attachment too large", exception.Message, "attachment_too_large"),
        AttachmentUnavailableException => new(503, "Attachment unavailable", exception.Message, "attachment_unavailable"),
        NotificationNotFoundException => new(404, "Notification not found", exception.Message, "notification_not_found"),
        NotificationValidationException => new(400, "Notification validation failed", exception.Message, "notification_validation_failed"),
        ActivityLogValidationException => new(400, "Activity log validation failed", exception.Message, "activity_log_validation_failed"),
        AiServiceUnavailableException => new(503, "AI analysis unavailable", exception.Message, "ai_service_unavailable"),
        AiProviderException => new(502, "AI provider failure", "AI analysis could not be completed.", "ai_provider_failed", true),
        ArgumentException => new(400, "Invalid request", "One or more request values are invalid.", "invalid_argument"),
        _ => new(500, "Internal server error", "An unexpected error occurred.", "internal_server_error", true)
    };

    private sealed record ExceptionMapping(
        int Status,
        string Title,
        string Detail,
        string Code,
        bool IsServerError = false);
}
