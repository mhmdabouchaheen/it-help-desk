using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Contracts.Auth;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace HelpDesk.Api.Tests;

public class AuthEndpointIntegrationTests
{
    [Fact]
    public async Task Register_Returns201AndExpectedResponse()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", ValidRegister());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal("test-access-token", body!.AccessToken);
        Assert.Equal("test-refresh-token", body.RefreshToken);
    }

    [Fact]
    public async Task Login_Returns200AndExpectedResponse()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", ValidLogin());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("test-access-token", (await response.Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken);
    }

    [Fact]
    public async Task Refresh_Returns200AndExpectedResponse()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/refresh", new RefreshTokenRequest { RefreshToken = "refresh" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("test-refresh-token", (await response.Content.ReadFromJsonAsync<AuthResponse>())!.RefreshToken);
    }

    [Fact]
    public async Task InvalidRegister_ReturnsValidationProblem()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new { });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        using var json = await ReadJsonAsync(response);
        Assert.True(TryProperty(json.RootElement, "errors", out _));
    }

    [Fact]
    public async Task InvalidLogin_Returns400()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { });
        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");
    }

    [Fact]
    public async Task EmptyRefreshToken_Returns400()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/refresh", new RefreshTokenRequest { RefreshToken = string.Empty });
        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "validation_failed");
    }

    [Fact]
    public async Task LogoutWithoutBearer_Returns401()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/logout", new LogoutRequest { RefreshToken = "refresh" });
        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "authentication_required");
    }

    [Fact]
    public async Task MeWithoutBearer_Returns401()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateClient().GetAsync("/api/auth/me");
        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "authentication_required");
    }

    [Fact]
    public async Task MeWithValidJwt_Returns200()
    {
        await using var factory = new AuthApiFactory();
        var client = AuthorizedClient(factory, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task MeUsesJwtSubjectClaim()
    {
        var userId = Guid.NewGuid();
        await using var factory = new AuthApiFactory();
        await AuthorizedClient(factory, userId).GetAsync("/api/auth/me");

        factory.AuthenticationService.Verify(service => service.GetCurrentUserAsync(
            userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutWithValidJwt_Returns204()
    {
        await using var factory = new AuthApiFactory();
        var response = await AuthorizedClient(factory, Guid.NewGuid()).PostAsJsonAsync(
            "/api/auth/logout", new LogoutRequest { RefreshToken = "refresh" });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateEmail_MapsTo409()
    {
        await using var factory = new AuthApiFactory();
        factory.AuthenticationService.Setup(service => service.RegisterAsync(
                It.IsAny<RegisterRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EmailAlreadyRegisteredException());
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", ValidRegister());
        await AssertProblemAsync(response, HttpStatusCode.Conflict, "email_already_registered");
    }

    [Fact]
    public async Task InvalidCredentials_MapToGeneric401()
    {
        await using var factory = new AuthApiFactory();
        factory.AuthenticationService.Setup(service => service.LoginAsync(
                It.IsAny<LoginRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthenticationFailedException());
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", ValidLogin());

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "authentication_failed");
        Assert.DoesNotContain("exist", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InactiveUser_MapsTo403()
    {
        await using var factory = new AuthApiFactory();
        factory.AuthenticationService.Setup(service => service.LoginAsync(
                It.IsAny<LoginRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UserInactiveException());
        await AssertProblemAsync(
            await factory.CreateClient().PostAsJsonAsync("/api/auth/login", ValidLogin()),
            HttpStatusCode.Forbidden,
            "user_inactive");
    }

    [Fact]
    public async Task InvalidRefreshToken_MapsTo401()
    {
        await using var factory = RefreshExceptionFactory(new InvalidRefreshTokenException());
        await AssertProblemAsync(
            await factory.CreateClient().PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest { RefreshToken = "invalid" }),
            HttpStatusCode.Unauthorized,
            "invalid_refresh_token");
    }

    [Fact]
    public async Task RefreshReuse_MapsTo401()
    {
        await using var factory = RefreshExceptionFactory(new RefreshTokenReuseDetectedException());
        await AssertProblemAsync(
            await factory.CreateClient().PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest { RefreshToken = "reused" }),
            HttpStatusCode.Unauthorized,
            "refresh_token_reuse_detected");
    }

    [Fact]
    public async Task MissingCurrentUser_MapsTo404()
    {
        await using var factory = new AuthApiFactory();
        factory.AuthenticationService.Setup(service => service.GetCurrentUserAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UserNotFoundException());
        await AssertProblemAsync(
            await AuthorizedClient(factory, Guid.NewGuid()).GetAsync("/api/auth/me"),
            HttpStatusCode.NotFound,
            "user_not_found");
    }

    [Fact]
    public async Task RegistrationFailure_MapsTo400()
    {
        await using var factory = RegisterExceptionFactory(new UserRegistrationException());
        await AssertProblemAsync(
            await factory.CreateClient().PostAsJsonAsync("/api/auth/register", ValidRegister()),
            HttpStatusCode.BadRequest,
            "registration_failed");
    }

    [Fact]
    public async Task TokenIssuanceFailure_MapsToGeneric500()
    {
        await using var factory = RegisterExceptionFactory(
            new AuthenticationTokenIssuanceException(new InvalidOperationException("internal detail")));
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", ValidRegister());

        await AssertProblemAsync(response, HttpStatusCode.InternalServerError, "token_issuance_failed");
        Assert.DoesNotContain("internal detail", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnexpectedException_MapsToGeneric500()
    {
        await using var factory = RegisterExceptionFactory(new InvalidOperationException("database detail"));
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", ValidRegister());

        await AssertProblemAsync(response, HttpStatusCode.InternalServerError, "internal_server_error");
        Assert.DoesNotContain("database detail", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProblemResponse_ContainsTraceId()
    {
        await using var factory = RegisterExceptionFactory(new EmailAlreadyRegisteredException());
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", ValidRegister());
        using var json = await ReadJsonAsync(response);
        Assert.True(TryProperty(json.RootElement, "traceId", out var traceId));
        Assert.False(string.IsNullOrWhiteSpace(traceId.GetString()));
    }

    [Fact]
    public async Task ProblemResponse_DoesNotContainPassword()
    {
        const string password = "NeverEchoThisPassword1!";
        await using var factory = RegisterExceptionFactory(new UserRegistrationException());
        var request = ValidRegister();
        request.Password = password;
        request.ConfirmPassword = password;
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", request);
        Assert.DoesNotContain(password, await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProblemResponse_DoesNotContainRefreshToken()
    {
        const string token = "never-echo-this-refresh-token";
        await using var factory = RefreshExceptionFactory(new InvalidRefreshTokenException());
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/refresh", new RefreshTokenRequest { RefreshToken = token });
        Assert.DoesNotContain(token, await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProblemResponse_DoesNotContainStackTrace()
    {
        await using var factory = RegisterExceptionFactory(new InvalidOperationException("failure"));
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", ValidRegister());
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("stackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(" at ", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SuccessfulResponse_DoesNotExposeIdentitySecurityFields()
    {
        await using var factory = new AuthApiFactory();
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", ValidLogin());
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("concurrencyStamp", body, StringComparison.OrdinalIgnoreCase);
    }

    private static AuthApiFactory RegisterExceptionFactory(Exception exception)
    {
        var factory = new AuthApiFactory();
        factory.AuthenticationService.Setup(service => service.RegisterAsync(
                It.IsAny<RegisterRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
        return factory;
    }

    private static AuthApiFactory RefreshExceptionFactory(Exception exception)
    {
        var factory = new AuthApiFactory();
        factory.AuthenticationService.Setup(service => service.RefreshAsync(
                It.IsAny<RefreshTokenRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
        return factory;
    }

    private static HttpClient AuthorizedClient(AuthApiFactory factory, Guid userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateJwt(userId));
        return client;
    }

    private static string CreateJwt(Guid userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AuthApiFactory.TestSecret));
        var token = new JwtSecurityToken(
            issuer: AuthApiFactory.TestIssuer,
            audience: AuthApiFactory.TestAudience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, userId.ToString())],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static RegisterRequest ValidRegister() => new()
    {
        Email = "employee@example.test",
        Password = "Password1!",
        ConfirmPassword = "Password1!",
        DisplayName = "Test Employee"
    };

    private static LoginRequest ValidLogin() => new()
    {
        Email = "employee@example.test",
        Password = "Password1!"
    };

    private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = await ReadJsonAsync(response);
        Assert.True(TryProperty(json.RootElement, "code", out var actualCode));
        Assert.Equal(code, actualCode.GetString());
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    private static bool TryProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.TryGetProperty(name, out value))
        {
            return true;
        }

        var pascalName = char.ToUpperInvariant(name[0]) + name[1..];
        return element.TryGetProperty(pascalName, out value);
    }
}
