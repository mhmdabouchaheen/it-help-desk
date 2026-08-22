using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using HelpDesk.Api.Application.Common.Exceptions;
using HelpDesk.Api.Contracts.Auth;
using Moq;

namespace HelpDesk.Api.Tests;

public sealed class PasswordResetAndProfileEndpointTests
{
    [Fact]
    public async Task ForgotPassword_UsesSameGenericContractForEveryEmail()
    {
        await using var factory = new AuthApiFactory();
        var client = factory.CreateClient();
        var existing = await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest { Email = "existing@example.test" });
        var missing = await client.PostAsJsonAsync("/api/auth/forgot-password", new ForgotPasswordRequest { Email = "missing@example.test" });
        Assert.Equal(HttpStatusCode.OK, existing.StatusCode);
        Assert.Equal(await existing.Content.ReadAsStringAsync(), await missing.Content.ReadAsStringAsync());
        Assert.DoesNotContain("token", await existing.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvalidResetToken_IsRejectedWithoutEchoingToken()
    {
        await using var factory = new AuthApiFactory();
        factory.AuthenticationService.Setup(x => x.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidPasswordResetException());
        const string token = "secret-reset-token";
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/reset-password", new ResetPasswordRequest { Email = "user@example.test", Token = token, NewPassword = "Password2!", ConfirmPassword = "Password2!" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(token, await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Profile_RequiresAuthentication()
    {
        await using var factory = new AuthApiFactory();
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/profile")).StatusCode);
    }

    [Fact]
    public async Task ProfileUpdate_UsesJwtSubjectAndLimitedContract()
    {
        var userId = Guid.NewGuid();
        await using var factory = new AuthApiFactory();
        var response = await AuthorizedClient(factory, userId).PutAsJsonAsync("/api/profile", new { displayName = "Updated", roles = new[] { "Admin" }, isActive = false, userId = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        factory.AuthenticationService.Verify(x => x.UpdateProfileAsync(userId, It.Is<UpdateProfileRequest>(r => r.DisplayName == "Updated"), It.IsAny<CancellationToken>()), Times.Once);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ChangePassword_UsesJwtSubject()
    {
        var userId = Guid.NewGuid();
        await using var factory = new AuthApiFactory();
        var request = new ChangePasswordRequest { CurrentPassword = "Password1!", NewPassword = "Password2!", ConfirmPassword = "Password2!" };
        Assert.Equal(HttpStatusCode.NoContent, (await AuthorizedClient(factory, userId).PostAsJsonAsync("/api/profile/change-password", request)).StatusCode);
        factory.AuthenticationService.Verify(x => x.ChangePasswordAsync(userId, It.IsAny<ChangePasswordRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static HttpClient AuthorizedClient(AuthApiFactory factory, Guid userId)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthApiFactory.CreateJwt(userId));
        return client;
    }
}
