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
        UserRegistrationException => new(400, "Registration failed", exception.Message, "registration_failed"),
        AuthenticationTokenIssuanceException => new(500, "Authentication service unavailable", "Authentication credentials could not be issued.", "token_issuance_failed", true),
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
